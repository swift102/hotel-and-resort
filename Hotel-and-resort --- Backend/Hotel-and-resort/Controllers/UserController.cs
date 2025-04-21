using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Hotel_and_resort.Models;
using Hotel_and_resort.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Text.RegularExpressions;
using hotel_and_resort.Models;

namespace Hotel_and_resort.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<UsersController> _logger;
        private readonly AppDbContext _context;
        private readonly ISmsSenderService _smsSender;

        public UsersController(UserManager<User> userManager, SignInManager<User> signInManager, IEmailSender emailSender, ISmsSenderService smsSender, ILogger<UsersController> logger, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _context = context;
            _smsSender = smsSender;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if (user == null)
            {
                return Unauthorized("Invalid login attempt.");
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, loginDto.Password, false, false);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, new AuthenticationProperties { IsPersistent = false }, "Login");

                
                return Ok(new { message = "Login successful", user, user.Id });
            }

            return Unauthorized("Invalid login attempt.");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logout successful" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var user = new User
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                // Option 1: Add a default role if RegisterDto doesn't have a Role property
                await _userManager.AddToRoleAsync(user, "User"); // Assigning a default role

                // Option 2: Add the Role property to RegisterDto if you want role selection
                await _userManager.AddToRoleAsync(user, registerDto.Role);

                return Ok(new { Message = "User registered successfully." });
            }

            return BadRequest(result.Errors);
        }


        [HttpPost("addUser")]
        public async Task<IActionResult> AddUser([FromBody] CreateUserDto createUserDto)
        {
            var validationErrors = ValidateCreateUserDto(createUserDto);
            if (validationErrors.Any())
            {
                return BadRequest(new { Errors = validationErrors });
            }

           

            var user = new User
            {
                UserName = createUserDto.UserName,
                Email = createUserDto.Email,
                Name = createUserDto.Name,
                Surname = createUserDto.Surname,
                ContactNumber = createUserDto.ContactNumber,
                UserProfileID = createUserDto.UserProfileID
            };

            var result = await _userManager.CreateAsync(user, createUserDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            user = await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == user.Id);

            try
            {
                await _smsSender.SendSmsAsync(user.ContactNumber, $"Dear {createUserDto.Name},\n\nYour user account has been created. Here are your login details:\n\nUsername: {createUserDto.UserName}\nPassword: {createUserDto.Password}\n\nBest regards,\nHotel and resort");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {ContactNumber}", user.ContactNumber);
            }

            return Ok(user);
        }

        [HttpGet("getAllUsers")]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            return Ok(users);
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserName == username);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }
            return Ok(user);
        }

        [HttpDelete("deleteUserByUsername/{username}")]
        public async Task<IActionResult> DeleteUserByUsername(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("User deleted successfully.");
        }

       


        private List<string> ValidateCreateUserDto(CreateUserDto dto)
        {
            var errors = new List<string>();

            if (!IsValidContactNumber(dto.ContactNumber))
            {
                errors.Add("Invalid contact number. It should be 10 digits long and start with a + and the country code.");
            }

            if (dto.Name.Length > 50)
            {
                errors.Add("Name should not exceed 50 characters.");
            }

            if (dto.Surname.Length > 50)
            {
                errors.Add("Surname should not exceed 50 characters.");
            }

            if (!IsValidEmail(dto.Email))
            {
                errors.Add("Invalid email. It should not exceed 100 characters and must include '@' and '.com'.");
            }

            if (dto.UserName.Length > 50)
            {
                errors.Add("Username should not exceed 50 characters.");
            }

            return errors;
        }

        private bool IsValidContactNumber(string contactNumber)
        {
            return !string.IsNullOrWhiteSpace(contactNumber) && Regex.IsMatch(contactNumber, @"^\+\d{10,}$");
        }

        private bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && email.Length <= 100 && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") && email.EndsWith(".com");
        }
    }
}

