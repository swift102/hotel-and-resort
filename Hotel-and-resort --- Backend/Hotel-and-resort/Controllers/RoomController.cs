using Ganss.Xss;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace hotel_and_resort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly RoomService _roomService;
        private readonly ILogger<RoomController> _logger;
        private readonly IHtmlSanitizer _sanitizer;

        public RoomController(IRepository repository, RoomService roomService, ILogger<RoomController> logger)
        {
            _repository = repository;
            _roomService = roomService;
            _logger = logger;
            _sanitizer = new HtmlSanitizer();
        }

        [HttpGet]
        public async Task<IActionResult> GetRooms()
        {
            try
            {
                var rooms = await _repository.GetRoomsAsync();
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rooms");
                return StatusCode(500, new { Error = "Failed to retrieve rooms." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoomById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid room ID: {RoomId}", id);
                    return BadRequest(new { Error = "Invalid room ID." });
                }

                var room = await _repository.GetRoomByIdAsync(id);
                if (room == null)
                {
                    _logger.LogWarning("Room not found: {RoomId}", id);
                    return NotFound(new { Error = "Room not found." });
                }
                return Ok(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving room {RoomId}", id);
                return StatusCode(500, new { Error = "Failed to retrieve room." });
            }
        }

        [HttpPost("book")]
        [Authorize(Roles = "Guest,Admin")]
        public async Task<IActionResult> BookRoom([FromBody] BookingRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Sanitize inputs
                model.RoomId = int.Parse(_sanitizer.Sanitize(model.RoomId.ToString()));
                model.CheckIn = model.CheckIn.Date;
                model.CheckOut = model.CheckOut.Date;

                // Validate dates
                if (model.CheckIn < DateTime.UtcNow.Date || model.CheckOut <= model.CheckIn)
                {
                    _logger.LogWarning("Invalid booking dates: CheckIn={CheckIn}, CheckOut={CheckOut}", model.CheckIn, model.CheckOut);
                    return BadRequest(new { Error = "Invalid check-in or check-out date." });
                }

                // Validate room availability
                var isAvailable = await _roomService.IsRoomAvailableAsync(model.RoomId, model.CheckIn, model.CheckOut);
                if (!isAvailable)
                {
                    _logger.LogWarning("Room {RoomId} not available for dates {CheckIn} to {CheckOut}",
                        model.RoomId, model.CheckIn, model.CheckOut);
                    return BadRequest(new { Error = "Room is not available for the selected dates." });
                }

                // Get user context
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized booking attempt");
                    return Unauthorized(new { Error = "User not authenticated." });
                }

                var userProfileId = User.FindFirst("UserProfileID")?.Value;
                if (string.IsNullOrEmpty(userProfileId) || !int.TryParse(userProfileId, out int profileId))
                {
                    _logger.LogWarning("UserProfileID not found or invalid for user {UserId}", userId);
                    return BadRequest(new { Error = "User profile not configured." });
                }

                var booking = new Booking
                {
                    RoomId = model.RoomId,
                    UserId = userId,
                    UserProfileID = profileId,
                    CheckIn = model.CheckIn,
                    CheckOut = model.CheckOut,
                    TotalPrice = await _roomService.CalculatePrice(model.RoomId, model.CheckIn, model.CheckOut),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.AddBookingAsync(booking);
                _logger.LogInformation("Room booked successfully: BookingId={BookingId}, UserId={UserId}", booking.Id, userId);
                return Ok(new { Message = "Room booked successfully.", BookingId = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking room {RoomId} for user {UserId}", model.RoomId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { Error = "Failed to book room." });
            }
        }
    }

    public class BookingRequest
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public DateTime CheckIn { get; set; }

        [Required]
        public DateTime CheckOut { get; set; }
    }
}