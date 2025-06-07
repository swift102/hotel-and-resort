using Ganss.Xss;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using hotel_and_resort.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static hotel_and_resort.ViewModels.AmenitiesDTOs;


namespace hotel_and_resort.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AmenityController : ControllerBase
    {
        private readonly AmenityService _amenityService;
        private readonly ILogger<AmenityController> _logger;
        private readonly IHtmlSanitizer _sanitizer;

        public AmenityController(AmenityService amenityService, ILogger<AmenityController> logger)
        {
            _amenityService = amenityService;
            _logger = logger;
            _sanitizer = new HtmlSanitizer();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AmenitiesDTOs.AmenityListDTO>>> GetAmenities([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Sanitize and validate query parameters
                if (page < 1 || pageSize < 1)
                {
                    _logger.LogWarning("Invalid pagination parameters: page={Page}, pageSize={PageSize}", page, pageSize);
                    return BadRequest(new { Error = "Page and page size must be positive integers." });
                }
                if (pageSize > 100)
                {
                    _logger.LogWarning("Page size too large: pageSize={PageSize}", pageSize);
                    return BadRequest(new { Error = "Page size cannot exceed 100." });
                }

                var amenities = await _amenityService.GetAmenitiesAsync(page, pageSize);
                return Ok(amenities);
            }
            catch (AmenityServiceException ex)
            {
                _logger.LogError(ex, "Error retrieving amenities");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AmenitiesDTOs.AmenityDetailsDTO>> GetAmenity(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid amenity ID: {AmenityId}", id);
                    return BadRequest(new { Error = "Invalid amenity ID." });
                }

                var amenity = await _amenityService.GetAmenityByIdAsync(id);
                if (amenity == null)
                {
                    _logger.LogWarning("Amenity not found: {AmenityId}", id);
                    return NotFound(new { Error = $"Amenity with ID {id} not found." });
                }
                return Ok(amenity);
            }
            catch (AmenityServiceException ex)
            {
                _logger.LogError(ex, "Error retrieving amenity {AmenityId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AmenitiesDTOs.AmenityListDTO>> AddAmenity([FromBody] AmenityCreateUpdateDTO amenityDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid amenity data provided.");
                return BadRequest(ModelState);
            }

            try
            {
                // Sanitize DTO inputs
                amenityDto.Name = _sanitizer.Sanitize(amenityDto.Name);
                amenityDto.Description = _sanitizer.Sanitize(amenityDto.Description);

                // Map DTO to Model
                var amenityModel = new Amenities
                {
                    Name = amenityDto.Name,
                    Description = amenityDto.Description
                };

                var addedAmenity = await _amenityService.AddAmenityAsync(amenityModel);
                _logger.LogInformation("Amenity created: {AmenityId}", addedAmenity.Id);
                return CreatedAtAction(nameof(GetAmenity), new { id = addedAmenity.Id }, addedAmenity);
            }
            catch (AmenityValidationException ex)
            {
                _logger.LogWarning("Validation error: {Message}", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
            catch (AmenityServiceException ex)
            {
                _logger.LogError(ex, "Error adding amenity");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAmenity(int id, [FromBody] AmenitiesDTOs.AmenityCreateUpdateDTO amenityDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid amenity data provided for update.");
                return BadRequest(ModelState);
            }

            try
            {
                // Sanitize DTO inputs
                amenityDto.Name = _sanitizer.Sanitize(amenityDto.Name);
                amenityDto.Description = _sanitizer.Sanitize(amenityDto.Description);

                // Map DTO to Model
                var amenityModel = new Amenities
                {
                    Id = id,
                    Name = amenityDto.Name,
                    Description = amenityDto.Description
                };

                await _amenityService.UpdateAmenityAsync(amenityModel);
                _logger.LogInformation("Amenity updated: {AmenityId}", id);
                return NoContent();
            }
            catch (AmenityValidationException ex)
            {
                _logger.LogWarning("Validation error: {Message}", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
            catch (AmenityServiceException ex)
            {
                _logger.LogError(ex, "Error updating amenity {AmenityId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid amenity ID: {AmenityId}", id);
                    return BadRequest(new { Error = "Invalid amenity ID." });
                }

                await _amenityService.DeleteAmenityAsync(id);
                _logger.LogInformation("Amenity deleted: {AmenityId}", id);
                return NoContent();
            }
            catch (AmenityServiceException ex)
            {
                _logger.LogError(ex, "Error deleting amenity {AmenityId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
