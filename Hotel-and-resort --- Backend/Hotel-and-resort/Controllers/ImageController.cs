using Ganss.Xss;
using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hotel_and_resort.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly IHtmlSanitizer _sanitizer;

        public ImageController(ILogger<ImageController> logger)
        {
            _logger = logger;
            _sanitizer = new HtmlSanitizer();
        }

        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> GetImages()
        {
            try
            {
                // Placeholder for fetching images
                _logger.LogInformation("Retrieved images for user {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return Ok(new { Message = "Images retrieved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images");
                return StatusCode(500, new { Error = "Failed to retrieve images." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Uploads Restricted to Admins
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Invalid file upload attempt by user {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                    return BadRequest(new { Error = "File is required." });
                }

                // Sanitize file name to prevent path traversal
                var fileName = _sanitizer.Sanitize(Path.GetFileName(file.FileName));
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    _logger.LogWarning("Invalid file name provided by user {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                    return BadRequest(new { Error = "Invalid file name." });
                }

                // Placeholder for saving file
                _logger.LogInformation("Image uploaded successfully by user {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return Ok(new { Message = "Image uploaded successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image by user {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { Error = "Failed to upload image." });
            }
        }
    }
}
