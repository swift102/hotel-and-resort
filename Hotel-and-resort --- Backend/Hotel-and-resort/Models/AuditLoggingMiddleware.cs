using System.Security.Claims;

namespace Hotel_and_resort.Models
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next; private readonly ILogger _logger;

        public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = context.User.FindFirst(ClaimTypes.Role)?.Value;
            var path = context.Request.Path;

            if (context.User.Identity.IsAuthenticated)
            {
                _logger.LogInformation("Authorization: User {UserId} with roles {Roles} accessed {Path}", userId, roles, path);
            }

            await _next(context);
        }
    }

}