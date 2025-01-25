using hotel_and_resort.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hotel_and_resort.ViewModels;
using static hotel_and_resort.ViewModels.AmenitiesDTOs;

namespace hotel_and_resort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmenityController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly ILogger<AmenityController> _logger;

        public AmenityController(IRepository repository, ILogger<AmenityController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: api/amenities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AmenityListDTO>>> GetAmenities()
        {
            try
            {
                var amenities = await _repository.GetAmenities();
                if (amenities == null || !amenities.Any())
                {
                    _logger.LogWarning("No amenities found.");
                    return NotFound();
                }

                var amenityListDTOs = amenities.Select(a => new AmenityListDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description
                }).ToList();

                return Ok(amenityListDTOs);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenities");
                return StatusCode(500, "Internal server error");
            }
        }


        // GET: api/amenities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AmenityDetailsDTO>> GetAmenity(int id)
        {
            try
            {
                var amenity = await _repository.GetAmenityById(id);
                if (amenity == null)
                {
                    _logger.LogWarning("Amenity not found for ID: {Id}", id);
                    return NotFound();
                }

                var amenityDetailsDTO = new AmenityDetailsDTO
                {
                    Id = amenity.Id,
                    Name = amenity.Name,
                    Description = amenity.Description,
                    Rooms = amenity.Rooms.Select(r => new RoomDTO
                    {
                        Id = r.ID,
                        //RoomNumber = r.RoomNumber,
                        //RoomType = r.RoomType
                    }).ToList()
                };

                return Ok(amenityDetailsDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenity");
                return StatusCode(500, "Internal server error");
            }
        }


        // POST: api/amenities
        [HttpPost]
        public async Task<ActionResult<AmenityListDTO>> AddAmenity(AmenityCreateUpdateDTO dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Name))
                {
                    _logger.LogWarning("Amenity name is missing.");
                    return BadRequest("Amenity name is required.");
                }

                var newAmenity = new Amenities
                {
                    Name = dto.Name,
                    Description = dto.Description
                };

                var addedAmenity = await _repository.AddAmenity(newAmenity);

                var resultDTO = new AmenityListDTO
                {
                    Id = addedAmenity.Id,
                    Name = addedAmenity.Name,
                    Description = addedAmenity.Description
                };

                return CreatedAtAction(nameof(GetAmenity), new { id = addedAmenity.Id }, resultDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error adding amenity");
                return StatusCode(500, "Internal server error");
            }
        }


        // PUT: api/amenities/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAmenity(int id, AmenityCreateUpdateDTO dto)
        {
            try
            {
                var amenityItem = await _repository.GetAmenityById(id);
                if (amenityItem == null)
                {
                    _logger.LogWarning("Amenity not found for ID: {Id}", id);
                    return NotFound();
                }

                // Update fields
                amenityItem.Name = dto.Name;
                amenityItem.Description = dto.Description;

                await _repository.UpdateAmenity(amenityItem);

                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error updating amenity");
                return StatusCode(500, "Internal server error");
            }
        }


        // DELETE: api/amenities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            try
            {
                var amenity = await _repository.GetAmenityById(id);
                if (amenity == null)
                {
                    _logger.LogWarning("Amenity not found for ID: {Id}", id);
                    return NotFound();
                }

                // Use the repository to delete the amenity
                await _repository.DeleteAmenity(id);

                _logger.LogInformation("Amenity deleted successfully: {AmenityId}", id);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting amenity");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
