using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
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
        private readonly IRepository _repository;
        private readonly ILogger<ImageController> _logger;

        public ImageController(IRepository repository, ILogger<ImageController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: api/images
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImageReadDTO>>> GetImages()
        {
            try
            {
                var images = await _repository.GetImages();
                if (images == null || !images.Any())
                {
                    _logger.LogWarning("No images found.");
                    return NotFound();
                }

                var imageDtos = images.Select(i => new ImageReadDTO
                {
                    Id = i.Id,
                    Name = i.Name,
                    ImagePath = i.ImagePath,
                    RoomID = i.RoomID
                });

                return Ok(imageDtos);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching images");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/images/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ImageReadDTO>> GetImage(int id)
        {
            try
            {
                var image = await _repository.GetImageById(id);
                if (image == null)
                {
                    _logger.LogWarning("Image not found for ID: {Id}", id);
                    return NotFound();
                }

                var imageDto = new ImageReadDTO
                {
                    Id = image.Id,
                    Name = image.Name,
                    ImagePath = image.ImagePath,
                    RoomID = image.RoomID
                };

                return Ok(imageDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching image");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/images
        [HttpPost]
        public async Task<ActionResult<ImageReadDTO>> AddImage(ImageCreateDTO imageDto)
        {
            try
            {
                _logger.LogInformation("Received request to add a new image: {ImageDetails}", imageDto);

                if (string.IsNullOrEmpty(imageDto.Name) || string.IsNullOrEmpty(imageDto.ImagePath))
                {
                    _logger.LogWarning("Image name or path is invalid.");
                    return BadRequest("Image name and path are required.");
                }

                var image = new Image
                {
                    Name = imageDto.Name,
                    ImagePath = imageDto.ImagePath,
                    RoomID = imageDto.RoomID
                };

                var addedImage = await _repository.AddImage(image);
                _logger.LogInformation("Image added successfully: {ImageId}", addedImage.Id);

                var createdImageDto = new ImageReadDTO
                {
                    Id = addedImage.Id,
                    Name = addedImage.Name,
                    ImagePath = addedImage.ImagePath,
                    RoomID = addedImage.RoomID
                };

                return CreatedAtAction(nameof(GetImage), new { id = addedImage.Id }, createdImageDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error adding image");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var filePath = Path.Combine("wwwroot/images", file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var image = new Image
            {
                Name = file.FileName,
                ImagePath = filePath,
                RoomID = 1 // Replace with actual room ID
            };

            await _repository.AddImage(image);
            return Ok(new { filePath });
        }

        // PUT: api/images/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateImage(int id, ImageUpdateDTO imageDto)
        {
            try
            {
                var image = await _repository.GetImageById(id);
                if (image == null)
                {
                    _logger.LogWarning("Image not found for ID: {Id}", id);
                    return NotFound();
                }

                image.Name = imageDto.Name;
                image.ImagePath = imageDto.ImagePath;
                image.RoomID = imageDto.RoomID;

                await _repository.UpdateImage(image);

                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error updating image");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/images/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            try
            {
                var image = await _repository.GetImageById(id);
                if (image == null)
                {
                    _logger.LogWarning("Image not found for ID: {Id}", id);
                    return NotFound();
                }

                await _repository.DeleteImage(id);
                _logger.LogInformation("Image deleted successfully: {ImageId}", id);

                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting image");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
