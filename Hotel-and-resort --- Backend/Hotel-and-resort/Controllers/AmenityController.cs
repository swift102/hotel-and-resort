using hotel_and_resort.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hotel_and_resort.ViewModels;
using static hotel_and_resort.ViewModels.AmenitiesDTOs;
using hotel_and_resort.Services;
using System.ComponentModel.DataAnnotations;


namespace hotel_and_resort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmenityController(AmenityService amenityService, ILogger<AmenityController> logger) : ControllerBase
    {
        private readonly AmenityService _amenityService = amenityService;
        private readonly ILogger<AmenityController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AmenityListDTO>>> GetAmenities(int page = 1, int pageSize = 10)
        {
            try
            {
                var amenities = await _amenityService.GetAmenitiesAsync(page, pageSize);
                if (!amenities.Any())
                {
                    _logger.LogWarning("No amenities found.");
                    return NotFound();
                }

                var amenityListDTOs = amenities.Select(a => new AmenityListDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description
                });

                return Ok(amenityListDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenities");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AmenityDetailsDTO>> GetAmenity(int id)
        {
            try
            {
                var amenity = await _amenityService.GetAmenityByIdAsync(id);
                if (amenity == null)
                {
                    _logger.LogWarning("Amenity not found for ID: {Id}", id);
                    return NotFound();
                }

                var amenities = await _amenityService.GetAmenitiesAsync();
                var amenityListDTOs = amenities.Select(a => new AmenityListDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description
                }).ToList();

                var amenityDetailsDTO = new AmenityDetailsDTO
                {
                    Id = amenity.Id,
                    Name = amenity.Name,
                    Description = amenity.Description,
                    Rooms = amenity.Rooms.Select(r => new RoomDTO
                    {
                        Id = r.Id,
                        Name = r.Name
                    }).ToList()
                };
                return Ok(amenityDetailsDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenity");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<AmenityListDTO>> AddAmenity([FromBody] AmenityCreateUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var newAmenity = new Amenities
                {
                    Name = dto.Name,
                    Description = dto.Description
                };

                var addedAmenity = await _amenityService.AddAmenityAsync(newAmenity);

                var resultDTO = new AmenityListDTO
                {
                    Id = addedAmenity.Id,
                    Name = addedAmenity.Name,
                    Description = addedAmenity.Description
                };

                return CreatedAtAction(nameof(GetAmenity), new { id = addedAmenity.Id }, resultDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding amenity");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAmenity(int id, [FromBody] AmenityCreateUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var amenity = await _amenityService.GetAmenityByIdAsync(id);
                if (amenity == null)
                {
                    _logger.LogWarning("Amenity not found for ID: {Id}", id);
                    return NotFound();
                }

                amenity.Name = dto.Name;
                amenity.Description = dto.Description;

                await _amenityService.UpdateAmenityAsync(amenity);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating amenity");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            try
            {
                var amenity = await _amenityService.GetAmenityByIdAsync(id);
                if (amenity == null)
                {
                    _logger.LogWarning("Amenity not found for ID: {Id}", id);
                    return NotFound();
                }

                await _amenityService.DeleteAmenityAsync(id);
                _logger.LogInformation("Amenity deleted successfully: {AmenityId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting amenity");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

    public class AmenityCreateUpdateDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }
        public string? Description { get; set; }

    }
