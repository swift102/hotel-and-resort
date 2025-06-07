using hotel_and_resort.Services;
using Hotel_and_resort.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using static hotel_and_resort.Models.Repository;

namespace Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            logger = _logger;
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
            _logger.LogError(ex, "Unhandled exception occurred for request {RequestPath}", context.Request.Path);

            var response = context.Response;
            response.ContentType = "application/json";

            var statusCode = ex switch
            {
                CustomerValidationException or AmenityValidationException or EmailValidationException => (int)HttpStatusCode.BadRequest,
                CustomerNotFoundException or RepositoryException => (int)HttpStatusCode.NotFound,
                DuplicateCustomerException => (int)HttpStatusCode.Conflict,
                UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
                _ => (int)HttpStatusCode.InternalServerError
            };

            response.StatusCode = statusCode;

            var errorResponse = new
            {
                Error = statusCode == (int)HttpStatusCode.InternalServerError
                    ? "An unexpected error occurred."
                    : ex.Message // Sanitized by custom exceptions
            };

            var json = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(json);
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}