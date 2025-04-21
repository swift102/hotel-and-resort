using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using Hotel_and_resort.Services.hotel_and_resort.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hotel_and_resort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly ILogger<RoomController> _logger;

        public RoomController(IRepository repository, ILogger<RoomController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: api/rooms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomReadDTO>>> GetRooms()
        {
            try
            {
                var rooms = await _repository.GetRooms();
                if (rooms == null || !rooms.Any())
                {
                    _logger.LogWarning("No rooms found.");
                    return NotFound();
                }

                var roomDtos = rooms.Select(r => new RoomReadDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Price = r.Price,
                    Capacity = r.Capacity,
                    Features = r.Features,
                    IsAvailable = r.IsAvailable
                });

                return Ok(roomDtos);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching rooms");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/rooms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomReadDTO>> GetRoom(int id)
        {
            try
            {
                var room = await _repository.GetRoomById(id);
                if (room == null)
                {
                    _logger.LogWarning("Room not found for ID: {Id}", id);
                    return NotFound();
                }

                var roomDto = new RoomReadDTO
                {
                    Id = room.Id,
                    Name = room.Name,
                    Description = room.Description,
                    Price = room.Price,
                    Capacity = room.Capacity,
                    Features = room.Features,
                    IsAvailable = room.IsAvailable
                };

                return Ok(roomDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching room");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/rooms
        [HttpPost]
        public async Task<ActionResult<RoomReadDTO>> AddRoom(RoomCreateDTO roomDto)
        {
            try
            {
                _logger.LogInformation("Received request to add a new room: {RoomDetails}", roomDto);

                if (string.IsNullOrEmpty(roomDto.Name) || roomDto.Price <= 0)
                {
                    _logger.LogWarning("Room name or price is invalid.");
                    return BadRequest("Room name and valid price are required.");
                }

                var room = new Room
                {
                    Name = roomDto.Name,
                    Description = roomDto.Description,
                    Price = roomDto.Price,
                    Capacity = roomDto.Capacity,
                    Features = roomDto.Features,
                    IsAvailable = roomDto.IsAvailable
                };

                var addedRoom = await _repository.AddRoom(room);
                _logger.LogInformation("Room added successfully: {RoomId}", addedRoom.Id);

                var createdRoomDto = new RoomReadDTO
                {
                    Id = addedRoom.Id,
                    Name = addedRoom.Name,
                    Description = addedRoom.Description,
                    Price = addedRoom.Price,
                    Capacity = addedRoom.Capacity,
                    Features = addedRoom.Features,
                    IsAvailable = addedRoom.IsAvailable
                };

                return CreatedAtAction(nameof(GetRoom), new { id = addedRoom.Id }, createdRoomDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error adding room");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/rooms/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, RoomUpdateDTO roomDto)
        {
            try
            {
                var room = await _repository.GetRoomById(id);
                if (room == null)
                {
                    _logger.LogWarning("Room not found for ID: {Id}", id);
                    return NotFound();
                }

                room.Name = roomDto.Name;
                room.Description = roomDto.Description;
                room.Price = roomDto.Price;
                room.Capacity = roomDto.Capacity;
                room.Features = roomDto.Features;
                room.IsAvailable = roomDto.IsAvailable;

                await _repository.UpdateRoom(room);

                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error updating room");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/rooms/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            try
            {
                var room = await _repository.GetRoomById(id);
                if (room == null)
                {
                    _logger.LogWarning("Room not found for ID: {Id}", id);
                    return NotFound();
                }

                await _repository.DeleteRoom(id);
                _logger.LogInformation("Room deleted successfully: {RoomId}", id);

                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting room");
                return StatusCode(500, "Internal server error");
            }
        }
    }
    namespace hotel_and_resort.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class RoomController : ControllerBase
        {
            private readonly RoomService _roomService;
            private readonly ILogger<RoomController> _logger;

            public RoomController(RoomService roomService, ILogger<RoomController> logger)
            {
                _roomService = roomService;
                _logger = logger;
            }

            [HttpGet]
            public async Task<ActionResult<IEnumerable<RoomReadDTO>>> GetRooms(int page = 1, int pageSize = 10)
            {
                try
                {
                    var rooms = await _roomService.GetRoomsAsync(page, pageSize);
                    if (!rooms.Any())
                    {
                        _logger.LogWarning("No rooms found.");
                        return NotFound();
                    }

                    var roomDtos = rooms.Select(r => new RoomReadDTO
                    {
                        Id = r.Id, // Changed from ID to Id
                        Name = r.Name,
                        Description = r.Description,
                        Price = r.Price,
                        Capacity = r.Capacity,
                        Features = r.Features,
                        IsAvailable = r.IsAvailable
                    });

                    return Ok(roomDtos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching rooms");
                    return StatusCode(500, "Internal server error");
                }
            }

            [HttpGet("{id}")]
            public async Task<ActionResult<RoomReadDTO>> GetRoom(int id)
            {
                try
                {
                    var room = await _roomService.GetRoomByIdAsync(id);
                    if (room == null)
                    {
                        _logger.LogWarning("Room not found for ID: {Id}", id);
                        return NotFound();
                    }

                    var roomDto = new RoomReadDTO
                    {
                        Id = room.Id,
                        Name = room.Name,
                        Description = room.Description,
                        Price = room.Price,
                        Capacity = room.Capacity,
                        Features = room.Features,
                        IsAvailable = room.IsAvailable
                    };

                    return Ok(roomDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching room");
                    return StatusCode(500, "Internal server error");
                }
            }

            [HttpPost]
            public async Task<ActionResult<RoomReadDTO>> AddRoom([FromBody] RoomCreateDTO roomDto)
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    var room = new Room
                    {
                        Name = roomDto.Name,
                        Description = roomDto.Description,
                        Price = roomDto.Price,
                        Capacity = roomDto.Capacity,
                        Features = roomDto.Features,
                        IsAvailable = roomDto.IsAvailable
                    };

                    var addedRoom = await _roomService.AddRoomAsync(room);
                    _logger.LogInformation("Room added successfully: {RoomId}", addedRoom.Id);

                    var createdRoomDto = new RoomReadDTO
                    {
                        Id = addedRoom.Id,
                        Name = addedRoom.Name,
                        Description = addedRoom.Description,
                        Price = addedRoom.Price,
                        Capacity = addedRoom.Capacity,
                        Features = addedRoom.Features,
                        IsAvailable = addedRoom.IsAvailable
                    };

                    return CreatedAtAction(nameof(GetRoom), new { id = addedRoom.Id }, createdRoomDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding room");
                    return StatusCode(500, "Internal server error");
                }
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateRoom(int id, [FromBody] RoomUpdateDTO roomDto)
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    var room = await _roomService.GetRoomByIdAsync(id);
                    if (room == null)
                    {
                        _logger.LogWarning("Room not found for ID: {Id}", id);
                        return NotFound();
                    }

                    room.Name = roomDto.Name;
                    room.Description = roomDto.Description;
                    room.Price = roomDto.Price;
                    room.Capacity = roomDto.Capacity;
                    room.Features = roomDto.Features;
                    room.IsAvailable = roomDto.IsAvailable;

                    await _roomService.UpdateRoomAsync(room);
                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating room");
                    return StatusCode(500, "Internal server error");
                }
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteRoom(int id)
            {
                try
                {
                    var room = await _roomService.GetRoomByIdAsync(id);
                    if (room == null)
                    {
                        _logger.LogWarning("Room not found for ID: {Id}", id);
                        return NotFound();
                    }

                    await _roomService.DeleteRoomAsync(id);
                    _logger.LogInformation("Room deleted successfully: {RoomId}", id);
                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting room");
                    return StatusCode(500, "Internal server error");
                }
            }
        }
    }
}
