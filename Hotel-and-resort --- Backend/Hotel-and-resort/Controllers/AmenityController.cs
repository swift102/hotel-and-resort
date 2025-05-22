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
    public class AmenityController(AmenityService amenityService, ILogger<AmenityController> logger) : ControllerBase
    {
        private readonly AmenityService _amenityService = amenityService;
        private readonly ILogger<AmenityController> _logger = logger;


            [HttpGet]
            public async Task<ActionResult<IEnumerable<Amenities>>> GetAmenities(int page = 1, int pageSize = 10)
            {
                try
                {
                    var amenities = await _amenityService.GetAmenitiesAsync(page, pageSize);
                    return Ok(amenities);
                }
                catch (AmenityServiceException ex)
                {
                    _logger.LogError(ex, "Error fetching amenities, page {Page}, pageSize {PageSize}", page, pageSize);
                    return StatusCode(500, new { Error = ex.Message });
                }
            }

            [HttpGet("{id}")]
            [Authorize]
            public async Task<ActionResult<Amenities>> GetAmenity(int id)
            {
                try
                {
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
                    _logger.LogError(ex, "Error fetching amenity with ID {AmenityId}", id);
                    return StatusCode(500, new { Error = ex.Message });
                }
            }

            [HttpPost]
            [Authorize(Roles = "Admin")]
            public async Task<ActionResult<Amenities>> AddAmenity([FromBody] AmenityCreateDTO dto)
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid amenity data provided.");
                    return BadRequest(ModelState);
                }

                try
                {
                    var amenity = new Amenities { Name = dto.Name, Description = dto.Description };
                    var added = await _amenityService.AddAmenityAsync(amenity);
                    _logger.LogInformation("Amenity created: {AmenityId}", added.Id);
                    return CreatedAtAction(nameof(GetAmenity), new { id = added.Id }, added);
                }
                catch (DuplicateAmenityException ex)
                {
                    _logger.LogWarning("Duplicate amenity error: {Message}", ex.Message);
                    return Conflict(new { Error = ex.Message });
                }
                catch (AmenityValidationException ex)
                {
                    _logger.LogWarning("Validation error: {Message}", ex.Message);
                    return BadRequest(new { Error = ex.Message });
                }
                catch (AmenityServiceException ex)
                {
                    _logger.LogError(ex, "Error adding amenity with name {Name}", dto.Name);
                    return StatusCode(500, new { Error = ex.Message });
                }
            }

            [HttpPut("{id}")]
            [Authorize(Roles = "Admin")]
            public async Task<IActionResult> UpdateAmenity(int id, [FromBody] AmenityUpdateDTO dto)
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid amenity data provided for update.");
                    return BadRequest(ModelState);
                }

                try
                {
                    var amenity = await _amenityService.GetAmenityByIdAsync(id);
                    if (amenity == null)
                    {
                        _logger.LogWarning("Amenity not found: {AmenityId}", id);
                        return NotFound(new { Error = $"Amenity with ID {id} not found." });
                    }

                    amenity.Name = dto.Name;
                    amenity.Description = dto.Description;

                    await _amenityService.UpdateAmenityAsync(amenity);
                    _logger.LogInformation("Amenity updated: {AmenityId}", id);
                    return NoContent();
                }
                catch (DuplicateAmenityException ex)
                {
                    _logger.LogWarning("Duplicate amenity error: {Message}", ex.Message);
                    return Conflict(new { Error = ex.Message });
                }
                catch (AmenityValidationException ex)
                {
                    _logger.LogWarning("Validation error: {Message}", ex.Message);
                    return BadRequest(new { Error = ex.Message });
                }
                catch (AmenityNotFoundException ex)
                {
                    _logger.LogWarning("Amenity not found: {Message}", ex.Message);
                    return NotFound(new { Error = ex.Message });
                }
                catch (AmenityServiceException ex)
                {
                    _logger.LogError(ex, "Error updating amenity with ID {AmenityId}", id);
                    return StatusCode(500, new { Error = ex.Message });
                }
            }

            [HttpDelete("{id}")]
            [Authorize(Roles = "Admin")]
            public async Task<IActionResult> DeleteAmenity(int id)
            {
                try
                {
                    var amenity = await _amenityService.GetAmenityByIdAsync(id);
                    if (amenity == null)
                    {
                        _logger.LogWarning("Amenity not found: {AmenityId}", id);
                        return NotFound(new { Error = $"Amenity with ID {id} not found." });
                    }

                    await _amenityService.DeleteAmenityAsync(id);
                    _logger.LogInformation("Amenity deleted: {AmenityId}", id);
                    return NoContent();
                }
                catch (AmenityNotFoundException ex)
                {
                    _logger.LogWarning("Amenity not found: {Message}", ex.Message);
                    return NotFound(new { Error = ex.Message });
                }
                catch (AmenityServiceException ex)
                {
                    _logger.LogError(ex, "Error deleting amenity with ID {AmenityId}", id);
                    return StatusCode(500, new { Error = ex.Message });
                }
            }
    }

        public class AmenityCreateDTO
        {
            [Required]
            [MaxLength(100)]
            public string Name { get; set; }

            [MaxLength(500)]
            public string? Description { get; set; }
        }

        public class AmenityUpdateDTO
        {
            [Required]
            [MaxLength(100)]
            public string Name { get; set; }

            [MaxLength(500)]
            public string? Description { get; set; }
        }
}
