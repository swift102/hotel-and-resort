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

        [HttpPost("forgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.UserName);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserName}", forgotPasswordDto.UserName);
                    return BadRequest("User not found.");
                }

                var verificationCode = new Random().Next(1000, 9999).ToString();
                user.VerificationCode = verificationCode;
                user.VerificationCodeExpiration = DateTime.UtcNow.AddMinutes(5);

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Could not save the verification code for user: {UserId}", user.Id);
                    return BadRequest("Could not save the verification code.");
                }

                await _emailSender.SendEmailAsync(user.Email, "Password Reset Verification Code", $"Your verification code is: {verificationCode}");
                _logger.LogInformation("Verification code sent to user: {UserId}", user.Id);

                return Ok(new { message = "Verification code sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing forgot password for user: {UserName}", forgotPasswordDto.UserName);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.FindByNameAsync(model.UserName);
                    if (user != null)
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var resetPasswordResult = await _userManager.ResetPasswordAsync(
                            user, token, model.NewPassword);

                        if (resetPasswordResult.Succeeded)
                        {
                            return Ok(new { Message = "Password reset successful" });
                        }
                        else
                        {
                            return BadRequest(new { Errors = resetPasswordResult.Errors.Select(e => e.Description) });
                        }
                    }
                    return NotFound("User not found.");
                }

                return BadRequest(new { Message = "Invalid data provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resetting password for user {model.UserName}: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("VerifyCode")]
        public async Task<IActionResult> VerifyCode([FromBody] VerificationModel model)
        {
            _logger.LogInformation($"Verifying code for user: {model.UserName}");
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.FindByNameAsync(model.UserName);
                    if (user != null)
                    {
                        _logger.LogInformation($"Found user: {model.UserName}");
                        _logger.LogInformation($"Stored verification code: {user.VerificationCode}, Provided verification code: {model.VerificationCode}");
                        _logger.LogInformation($"Current time: {DateTime.UtcNow}, Code expiration time: {user.VerificationCodeExpiration}");

                        if (user.VerificationCode == model.VerificationCode && user.VerificationCodeExpiration > DateTime.UtcNow)
                        {
                            _logger.LogInformation("Verification code is valid");
                            return Ok(new { isValid = true });
                        }
                        else
                        {
                            if (user.VerificationCode != model.VerificationCode)
                            {
                                _logger.LogWarning("Verification code does not match");
                            }
                            if (user.VerificationCodeExpiration <= DateTime.UtcNow)
                            {
                                _logger.LogWarning("Verification code has expired");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"User not found: {model.UserName}");
                    }
                }
                else
                {
                    _logger.LogWarning("Model state is invalid");
                }

                return Ok(new { isValid = false });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying code for user {model.UserName}: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Add the SendVerificationEmail method here
        [HttpPost("SendVerificationEmail")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "ResetPassword");
                _logger.LogInformation($"Generated token for user {email}: {token}");

                // Code to send the token via email
                await _emailSender.SendEmailAsync(user.Email, "Verification Code", $"Your verification code is: {token}");

                return Ok(new { Message = "Verification email sent." });
            }
            return BadRequest(new { Message = "Invalid email address." });
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

