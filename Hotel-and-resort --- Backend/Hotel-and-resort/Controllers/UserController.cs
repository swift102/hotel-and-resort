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
using hotel_and_resort.Services;
using Microsoft.AspNetCore.Authorization;



// Add an alias for one of the conflicting namespaces
using IEmailSender = hotel_and_resort.Services.IEmailSender;



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

        public UsersController(UserManager<User> userManager, SignInManager<User> signInManager,IEmailSender emailSender, ISmsSenderService smsSender, ILogger<UsersController> logger, AppDbContext context)
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
                await _userManager.AddToRoleAsync(user, registerDto.Role ?? "Guest");
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

        [HttpGet("admin/users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsersWithRoles()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userDtos = new List<object>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userDtos.Add(new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.Name,
                        user.Surname,
                        user.ContactNumber,
                        user.UserProfileID,
                        Roles = roles
                    });
                }
                _logger.LogInformation("Admin retrieved {UserCount} users", userDtos.Count);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all users for admin");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPut("admin/users/{id}/roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] List<string> roles)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for role update", id);
                    return NotFound(new { Error = "User not found." });
                }

                var validRoles = new[] { "Admin", "Staff", "Guest" };
                if (roles.Any(r => !validRoles.Contains(r)))
                {
                    _logger.LogWarning("Invalid roles provided for User {UserId}: {Roles}", id, string.Join(", ", roles));
                    return BadRequest(new { Error = "Invalid roles. Must be Admin, Staff, or Guest." });
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRolesAsync(user, roles);

                _logger.LogInformation("Admin updated roles for User {UserId} to {Roles}", id, string.Join(", ", roles));
                return Ok(new { Message = "User roles updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roles for User {UserId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }
    }
}

