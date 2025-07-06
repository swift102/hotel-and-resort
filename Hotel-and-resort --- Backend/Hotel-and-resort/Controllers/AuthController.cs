using Ganss.Xss;
using BCrypt.Net;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Hotel_and_resort.Models;
using Hotel_and_resort.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Hotel_and_resort.Data;

namespace hotel_and_resort.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ICustomerService _customerService;
        private readonly ILogger<AuthController> _logger;
        private readonly TokenService _tokenService;
        private readonly AppDbContext _context;
        private readonly IHtmlSanitizer _sanitizer;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            ICustomerService customerService,
            ILogger<AuthController> logger,
            TokenService tokenService,
            AppDbContext context,
            IHtmlSanitizer sanitizer)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login DTO: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(new ErrorResponse { Message = "Invalid login data", Details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                loginDto.UserName = _sanitizer.Sanitize(loginDto.UserName);

                var user = await _userManager.FindByNameAsync(loginDto.UserName);
                if (user == null)
                {
                    _logger.LogWarning("Invalid login attempt for username: {UserName}", loginDto.UserName);
                    return Unauthorized(new ErrorResponse { Message = "Invalid login attempt" });
                }

                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    _logger.LogWarning("Email not confirmed for user: {UserName}", loginDto.UserName);
                    return Unauthorized(new ErrorResponse { Message = "Email not confirmed. Please check your inbox." });
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, loginDto.Password, false, false);
                if (result.Succeeded)
                {
                    var customer = await _customerService.GetCustomerByUserIdAsync(user.Id);
                    if (customer == null)
                    {
                        _logger.LogWarning("No customer record found for user: {UserId}", user.Id);
                        return Unauthorized(new ErrorResponse { Message = "User profile not fully configured." });
                    }

                    var userProfile = await _context.UserProfiles.FindAsync(user.UserProfileID);
                    if (userProfile == null)
                    {
                        _logger.LogWarning("No user profile found for user: {UserId}", user.Id);
                        return Unauthorized(new ErrorResponse { Message = "User profile not fully configured." });
                    }

                    var roles = await _userManager.GetRolesAsync(user);
                    var accessToken = _tokenService.GenerateToken(user, roles);
                    var refreshToken = Guid.NewGuid().ToString();

                    await StoreRefreshToken(user, refreshToken);

                    _logger.LogInformation("User logged in: {UserId}", user.Id);
                    return Ok(new
                    {
                        Message = "Login successful",
                        UserId = user.Id,
                        UserProfileID = user.UserProfileID,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        Roles = roles
                    });
                }

                _logger.LogWarning("Invalid login attempt for username: {UserName}", loginDto.UserName);
                return Unauthorized(new ErrorResponse { Message = "Invalid login attempt" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing login for {UserName}", loginDto.UserName);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration DTO: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(new ErrorResponse { Message = "Invalid registration data", Details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                registerDto.UserName = _sanitizer.Sanitize(registerDto.UserName);
                registerDto.Email = _sanitizer.Sanitize(registerDto.Email);
                registerDto.Name = _sanitizer.Sanitize(registerDto.Name);
                registerDto.Surname = _sanitizer.Sanitize(registerDto.Surname);
                registerDto.ContactNumber = _sanitizer.Sanitize(registerDto.ContactNumber);
                registerDto.ProfileDescription = _sanitizer.Sanitize(registerDto.ProfileDescription);

                var user = new User
                {
                    UserName = registerDto.UserName,
                    Email = registerDto.Email,
                    Name = registerDto.Name,
                    Surname = registerDto.Surname,
                    ContactNumber = registerDto.ContactNumber
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);
                if (result.Succeeded)
                {
                    var role = User.IsInRole("Admin") && !string.IsNullOrEmpty(registerDto.Role) ? registerDto.Role : "Guest";
                    if (!new[] { "Admin", "Staff", "Guest" }.Contains(role))
                    {
                        _logger.LogWarning("Invalid role {Role} for user {UserName}", role, registerDto.UserName);
                        return BadRequest(new ErrorResponse { Message = "Invalid role. Must be Admin, Staff, or Guest." });
                    }
                    await _userManager.AddToRoleAsync(user, role);

                    // Create Customer record
                    var customer = new Customer
                    {
                        FirstName = registerDto.Name ?? registerDto.UserName,
                        LastName = registerDto.Surname ?? "User",
                        Email = registerDto.Email,
                        Phone = registerDto.ContactNumber ?? "000-000-0000",
                        UserId = user.Id
                    };
                    customer = await _customerService.AddCustomerAsync(customer);

                    // Create UserProfile
                    var userProfile = new UserProfile
                    {
                        UserProfileID = customer.Id, 
                        ProfileDescription = registerDto.ProfileDescription
                    };
                    await _context.UserProfiles.AddAsync(userProfile);
                    user.UserProfileID = userProfile.UserProfileID;
                    await _userManager.UpdateAsync(user);
                    await _context.SaveChangesAsync();

                    // Generate email confirmation token
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { userId = user.Id, token }, Request.Scheme);

                    var emailBody = _sanitizer.Sanitize(
                        $"<h3>Welcome to Hotel and Resort</h3><p>Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.</p>");
                    await _emailSender.SendEmailAsync(user.Email, "Confirm Your Email", emailBody);

                    _logger.LogInformation("User registered: {UserId}, Role: {Role}", user.Id, role);
                    return Ok(new { Message = "User registered successfully. Please confirm your email.", Role = role, UserProfileID = user.UserProfileID });
                }

                return BadRequest(new ErrorResponse { Message = "Registration failed", Details = result.Errors.Select(e => e.Description) });
            }
            catch (DuplicateCustomerException ex)
            {
                _logger.LogWarning("Duplicate customer during registration: {Message}", ex.Message);
                return Conflict(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing registration for {UserName}", registerDto.UserName);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out: {UserId}", userId);
                return Ok(new { Message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing logout");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid forgot password DTO: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(new ErrorResponse { Message = "Invalid email", Details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                forgotPasswordDto.Email = _sanitizer.Sanitize(forgotPasswordDto.Email);

                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {Email}", forgotPasswordDto.Email);
                    return BadRequest(new ErrorResponse { Message = "User not found" });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = $"https://yourwebsite.com/reset-password?token={token}&email={user.Email}";
                var emailBody = _sanitizer.Sanitize(
                    $"<h3>Password Reset</h3><p>Use this link to reset your password: <a href='{resetLink}'>Reset Password</a></p>");

                await _emailSender.SendEmailAsync(user.Email, "Password Reset", emailBody);

                _logger.LogInformation("Password reset token sent to {Email}", user.Email);
                return Ok(new { Message = "Reset link sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for {Email}", forgotPasswordDto.Email);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid reset password DTO: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(new ErrorResponse { Message = "Invalid reset data", Details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                resetDto.Email = _sanitizer.Sanitize(resetDto.Email);
                resetDto.Token = _sanitizer.Sanitize(resetDto.Token);

                var user = await _userManager.FindByEmailAsync(resetDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {Email}", resetDto.Email);
                    return NotFound(new ErrorResponse { Message = "User not found" });
                }

                var result = await _userManager.ResetPasswordAsync(user, resetDto.Token, resetDto.NewPassword);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for {Email}", resetDto.Email);
                    return Ok(new { Message = "Password reset successful" });
                }

                return BadRequest(new ErrorResponse { Message = "Password reset failed", Details = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", resetDto.Email);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("refresh")]
        [Authorize]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid refresh token DTO: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(new ErrorResponse { Message = "Invalid refresh data", Details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var principal = _tokenService.GetPrincipalFromExpiredToken(model.AccessToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Invalid token for refresh request");
                    return Unauthorized(new ErrorResponse { Message = "Invalid token" });
                }

                var storedToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.UserId == user.Id && t.ExpiryDate > DateTime.UtcNow);
                if (storedToken == null || !BCrypt.Net.BCrypt.Verify(model.RefreshToken, storedToken.Token))
                {
                    _logger.LogWarning("Invalid or expired refresh token for user {UserId}", user.Id);
                    return Unauthorized(new ErrorResponse { Message = "Invalid or expired refresh token" });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.GenerateToken(user, roles);
                var newRefreshToken = Guid.NewGuid().ToString();

                await UpdateRefreshToken(user, storedToken, newRefreshToken);

                _logger.LogInformation("Token refreshed for user {UserId}", user.Id);
                return Ok(new
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken
                });
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Invalid token for refresh request: {Message}", ex.Message);
                return Unauthorized(new ErrorResponse { Message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing token refresh");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }


        [HttpGet("confirm-email")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Invalid email confirmation link: userId={UserId}, token={Token}", userId, token);
                    return BadRequest(new ErrorResponse { Message = "Invalid confirmation link" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email confirmation: {UserId}", userId);
                    return BadRequest(new ErrorResponse { Message = "User not found" });
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Email confirmed for user {UserId}", userId);
                    return Ok(new { Message = "Email confirmed successfully" });
                }

                return BadRequest(new ErrorResponse { Message = "Email confirmation failed", Details = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        private async Task StoreRefreshToken(User user, string refreshToken)
        {
            var hashedToken = BCrypt.Net.BCrypt.HashPassword(refreshToken); // Corrected usage of HashPassword
            var token = new RefreshToken
            {
                UserId = user.Id,
                Token = hashedToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateRefreshToken(User user, RefreshToken oldToken, string newRefreshToken)
        {
            _context.RefreshTokens.Remove(oldToken);
           var hashedToken = BCrypt.Net.BCrypt.HashPassword(newRefreshToken); 
            var newToken = new RefreshToken
            {
                UserId = user.Id,
                Token = hashedToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };
            await _context.RefreshTokens.AddAsync(newToken);
            await _context.SaveChangesAsync();
        }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
        public IEnumerable<string> Details { get; set; }
    }

    public class LoginDto
    {
        [Required, MaxLength(50)]
        public string UserName { get; set; }
        [Required, MinLength(8)]
        public string Password { get; set; }
    }

    public class RegisterDto
    {
        [Required, MaxLength(50)]
        public string UserName { get; set; }
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; }
        [Required, MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "Password must have at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; }
        public string Role { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(100)]
        public string Surname { get; set; }
        [Phone, MaxLength(20)]
        public string ContactNumber { get; set; }
        [MaxLength(500)]
        public string ProfileDescription { get; set; }
    }

    public class ForgotPasswordDto
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; }
        [Required]
        public string Token { get; set; }
        [Required, MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "Password must have at least one uppercase letter, one lowercase letter, one number, and one special character.")]
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