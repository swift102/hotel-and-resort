using Ganss.Xss;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace hotel_and_resort.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly Services.IEmailSender _emailSender;
        private readonly ILogger<AuthController> _logger;
        private readonly TokenService _tokenService;
        private readonly AppDbContext _context;
        private readonly IHtmlSanitizer _sanitizer;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            Services.IEmailSender emailSender,
            ILogger<AuthController> logger,
            TokenService tokenService,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _tokenService = tokenService;
            _context = context;
            _sanitizer = new HtmlSanitizer();
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Sanitize inputs
                loginDto.UserName = _sanitizer.Sanitize(loginDto.UserName);

                var user = await _userManager.FindByNameAsync(loginDto.UserName);
                if (user == null)
                {
                    _logger.LogWarning("Invalid login attempt for username: {UserName}", loginDto.UserName);
                    return Unauthorized("Invalid login attempt.");
                }

                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    _logger.LogWarning("Email not confirmed for user: {UserName}", loginDto.UserName);
                    return Unauthorized("Email not confirmed. Please check your inbox.");
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, loginDto.Password, false, false);
                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var accessToken = _tokenService.GenerateToken(user, roles);
                    var refreshToken = Guid.NewGuid().ToString();

                    // Store refresh token
                    await StoreRefreshToken(user, refreshToken);

                    return Ok(new
                    {
                        Message = "Login successful",
                        UserId = user.Id,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        Roles = roles
                    });
                }

                _logger.LogWarning("Invalid login attempt for username: {UserName}", loginDto.UserName);
                return Unauthorized("Invalid login attempt.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing login for {UserName}", loginDto.UserName);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Sanitize inputs
                registerDto.UserName = _sanitizer.Sanitize(registerDto.UserName);
                registerDto.Email = _sanitizer.Sanitize(registerDto.Email);

                var user = new User
                {
                    UserName = registerDto.UserName,
                    Email = registerDto.Email,
                    Name = registerDto.Name,
                    Surname = registerDto.Surname,
                    UserProfileID = registerDto.UserProfileID ?? 0
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);
                if (result.Succeeded)
                {
                    var role = registerDto.Role ?? "Guest";
                    if (!new[] { "Admin", "Staff", "Guest" }.Contains(role))
                    {
                        _logger.LogWarning("Invalid role {Role} for user {UserName}", role, registerDto.UserName);
                        return BadRequest("Invalid role. Must be Admin, Staff, or Guest.");
                    }
                    await _userManager.AddToRoleAsync(user, role);

                    // Generate email confirmation token
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { userId = user.Id, token }, Request.Scheme);

                    // Send confirmation email
                    var emailBody = _sanitizer.Sanitize(
                        $"<h3>Welcome to Hotel and Resort</h3><p>Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.</p>");
                    await _emailSender.SendEmailAsync(user.Email, "Confirm Your Email", emailBody);

                    return Ok(new { Message = "User registered successfully. Please confirm your email.", Role = role });
                }

                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing registration for {UserName}", registerDto.UserName);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                return Ok(new { Message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing logout");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // Sanitize input
                forgotPasswordDto.Email = _sanitizer.Sanitize(forgotPasswordDto.Email);

                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {Email}", forgotPasswordDto.Email);
                    return BadRequest("User not found.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = $"https://yourwebsite.com/reset-password?token={token}&email={user.Email}";
                var emailBody = _sanitizer.Sanitize(
                    $"<h3>Password Reset</h3><p>Use this link to reset your password: <a href='{resetLink}'>Reset Password</a></p>");

                await _emailSender.SendEmailAsync(user.Email, "Password Reset", emailBody);

                _logger.LogInformation("Password reset token sent to {Email}", user.Email);
                return Ok(new { Message = "Reset link sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for {Email}", forgotPasswordDto.Email);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // Sanitize inputs
                resetDto.Email = _sanitizer.Sanitize(resetDto.Email);
                resetDto.Token = _sanitizer.Sanitize(resetDto.Token);

                var user = await _userManager.FindByEmailAsync(resetDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {Email}", resetDto.Email);
                    return NotFound("User not found.");
                }

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
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var principal = _tokenService.GetPrincipalFromExpiredToken(model.AccessToken);
                var email = principal.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Invalid token for refresh request");
                    return Unauthorized(new { Error = "Invalid token" });
                }

                var storedToken = _context.RefreshTokens
                    .FirstOrDefault(t => t.UserId == user.Id && t.Token == model.RefreshToken && t.ExpiryDate > DateTime.UtcNow);
                if (storedToken == null)
                {
                    _logger.LogWarning("Invalid or expired refresh token for user {UserId}", user.Id);
                    return Unauthorized(new { Error = "Invalid or expired refresh token" });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var newAccessToken = _tokenService.GenerateToken(user, roles);
                var newRefreshToken = Guid.NewGuid().ToString();

                await UpdateRefreshToken(user, storedToken, newRefreshToken);

                return Ok(new
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing token refresh");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Invalid email confirmation link: userId={UserId}, token={Token}", userId, token);
                    return BadRequest(new { Error = "Invalid confirmation link" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email confirmation: {UserId}", userId);
                    return BadRequest(new { Error = "User not found" });
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    return Ok(new { Message = "Email confirmed successfully" });
                }

                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        private async Task StoreRefreshToken(User user, string refreshToken)
        {
            var token = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateRefreshToken(User user, RefreshToken oldToken, string newRefreshToken)
        {
            _context.RefreshTokens.Remove(oldToken);
            var newToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };
            _context.RefreshTokens.Add(newToken);
            await _context.SaveChangesAsync();
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
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public int? UserProfileID { get; set; }
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

    public class RefreshTokenDto
    {
        [Required]
        public string AccessToken { get; set; }
        [Required]
        public string RefreshToken { get; set; }
    }
}