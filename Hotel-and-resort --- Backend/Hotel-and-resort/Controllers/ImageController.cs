using Ganss.Xss;
using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using Hotel_and_resort.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _context;

        public ImageController(ILogger<ImageController> logger, IWebHostEnvironment environment, AppDbContext context)
        {
            _logger = logger;
            _sanitizer = new HtmlSanitizer();
            _environment = environment;
            _context = context;
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

        [Authorize(Roles = "Admin,Manager")] 
        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetRoomImages(int roomId)
        {
            var images = await _context.Images
                .Where(i => i.RoomID == roomId)
                .Select(i => new
                {
                    i.Id,
                    i.Name,
                    ImageUrl = $"{Request.Scheme}://{Request.Host}/images/rooms/{i.ImagePath}",
                    i.AltText
                })
                .ToListAsync();

            return Ok(images);
        }



        [HttpPost]
        [Authorize(Roles = "Admin")] // Uploads Restricted to Admins
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, int roomId, string roomType)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Invalid file type");

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";

            // Create directory path
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "rooms", roomType);
            Directory.CreateDirectory(uploadsFolder);

            // Full file path
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save to database
            var image = new Image
            {
                Name = file.FileName,
                ImagePath = $"{roomType}/{fileName}",
                RoomID = roomId
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            return Ok(new { imageId = image.Id, path = image.FullImageUrl });
        }


    }
}
