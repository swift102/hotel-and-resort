using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace hotel_and_resort.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IConfiguration configuration,
            UserManager<User> userManager,
            ILogger<TokenService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateToken(User user, IList<string> roles)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            if (!jwtSettings.GetChildren().Any())
            {
                _logger.LogError("JWT settings are missing in configuration.");
                throw new InvalidOperationException("JWT settings are missing.");
            }

            var keyString = jwtSettings["Key"];
            if (string.IsNullOrEmpty(keyString))
            {
                _logger.LogError("JWT Key is missing in configuration.");
                throw new InvalidOperationException("JWT Key is missing.");
            }
            var key = Encoding.ASCII.GetBytes(keyString);

            var expiryMinutes = 15.0; // Default
            if (!string.IsNullOrEmpty(jwtSettings["ExpiryInMinutes"]) &&
                !double.TryParse(jwtSettings["ExpiryInMinutes"], out expiryMinutes))
            {
                _logger.LogWarning("Invalid JWT ExpiryInMinutes: {Value}. Using default: 15 minutes.", jwtSettings["ExpiryInMinutes"]);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new("Name", user.Name ?? string.Empty),
                new("Surname", user.Surname ?? string.Empty),
                new("UserProfileID", user.UserProfileID.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            _logger.LogInformation("Generated JWT for user {UserId}", user.Id);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["Key"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid token algorithm for refresh token.");
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate expired token.");
                throw new SecurityTokenException("Invalid token", ex);
            }
        }
    }
}