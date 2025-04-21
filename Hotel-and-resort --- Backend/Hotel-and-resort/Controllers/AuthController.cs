using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Hotel_and_resort.Models;
using Hotel_and_resort.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using hotel_and_resort.Models;
using System.ComponentModel.DataAnnotations;


namespace hotel_and_resort.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager,
            IEmailSender emailSender, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if (user == null) return Unauthorized("Invalid login attempt.");

            var result = await _signInManager.PasswordSignInAsync(user.UserName, loginDto.Password, false, false);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, new AuthenticationProperties { IsPersistent = false });
                return Ok(new { Message = "Login successful", UserId = user.Id });
            }

            return Unauthorized("Invalid login attempt.");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "Logout successful" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = new User
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, registerDto.Role ?? "Guest");
                return Ok(new { Message = "User registered successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {Email}", forgotPasswordDto.Email);
                    return BadRequest("User not found.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _emailSender.SendEmailAsync(user.Email, "Password Reset",
                    $"Use this link to reset your password: https://yourwebsite.com/reset-password?token={token}&email={user.Email}");

                _logger.LogInformation("Password reset token sent to {Email}", user.Email);
                return Ok(new { Message = "Reset link sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for {Email}", forgotPasswordDto.Email);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var user = await _userManager.FindByEmailAsync(resetDto.Email);
                if (user == null) return NotFound("User not found.");

                var result = await _userManager.ResetPasswordAsync(user, resetDto.Token, resetDto.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(new { Message = "Password reset successful" });
                }

                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", resetDto.Email);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

public class LoginDto
{
    [Required, MaxLength(50)]
    public string UserName { get; set; }
    [Required]
    public string Password { get; set; }
}

public class RegisterDto
{
    [Required, MaxLength(50)]
    public string UserName { get; set; }
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    public string? Role { get; set; }
}

public class ForgotPasswordDto
{
    [Required, EmailAddress]
    public string Email { get; set; }
}

public class ResetPasswordDto
{
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Token { get; set; }
    [Required]
    public string NewPassword { get; set; }
}