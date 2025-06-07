using AspNetCoreRateLimit;
using hotel_and_resort.Services;
using Hotel_and_resort.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;




namespace Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred for request {RequestPath} from IP {RemoteIpAddress}",
                context.Request.Path, context.Connection.RemoteIpAddress);

            var response = context.Response;
            response.ContentType = "application/json";

            var (statusCode, message, includeDetails) = ex switch
            {
               
                CustomerValidationException or AmenityValidationException or RoomValidationException =>
                    ((int)HttpStatusCode.BadRequest, ex.Message, false),
                CustomerNotFoundException or RoomNotFoundException or BookingNotFoundException =>
                    ((int)HttpStatusCode.NotFound, ex.Message, false),
                DuplicateCustomerException =>
                    ((int)HttpStatusCode.Conflict, ex.Message, false),
                UnauthorizedAccessException =>
                    ((int)HttpStatusCode.Forbidden, "Access denied.", false),
                ArgumentException =>
                    ((int)HttpStatusCode.BadRequest, "Invalid request parameters.", false),
                TimeoutException =>
                    ((int)HttpStatusCode.RequestTimeout, "Request timeout occurred.", false),
                _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", false)
            };

            response.StatusCode = statusCode;

            var errorResponse = new ErrorResponse
            {
                Error = message,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Details = includeDetails && IsDevelopmentEnvironment() ? ex.StackTrace : null
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await response.WriteAsync(json);
        }


        private async Task HandleRateLimitHeaders(HttpContext context, Exception rateLimitEx)
        {
            // Try to get rate limit info from various sources
            var retryAfter = GetRetryAfterSeconds(context, rateLimitEx);

            if (retryAfter.HasValue)
            {
                context.Response.Headers.Append("Retry-After", retryAfter.Value.ToString());
            }

            // Add additional rate limit headers for better client handling
            if (context.Items.TryGetValue("X-RateLimit-Limit", out var limit))
            {
                context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
            }

            if (context.Items.TryGetValue("X-RateLimit-Remaining", out var remaining))
            {
                context.Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString());
            }
        }

        private int? GetRetryAfterSeconds(HttpContext context, Exception rateLimitEx)
        {
            // Try different approaches to get retry-after information
            if (context.Items.TryGetValue("RateLimitInfo", out var rateLimitInfo))
            {
                // Custom rate limit info object
                if (rateLimitInfo is RateLimitInfo info)
                {
                    return info.RetryAfterSeconds;
                }

                // Generic object with RetryAfter property
                var retryAfterProperty = rateLimitInfo.GetType().GetProperty("RetryAfter");
                if (retryAfterProperty != null)
                {
                    var value = retryAfterProperty.GetValue(rateLimitInfo);
                    if (value is int seconds)
                        return seconds;
                    if (value is TimeSpan timeSpan)
                        return (int)timeSpan.TotalSeconds;
                }
            }

            // Fallback: parse from exception message or use default
            return 60; // Default retry after 60 seconds
        }


      

        private bool IsDevelopmentEnvironment()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        }
    }

    // ErrorResponse.cs - Response model
    public class ErrorResponse
    {
        public string Error { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string Path { get; set; }
        public string Details { get; set; }
    }

    // RateLimitInfo.cs - Custom rate limit info class
    public class RateLimitInfo
    {
        public int RetryAfterSeconds { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public DateTime ResetTime { get; set; }
    }
}