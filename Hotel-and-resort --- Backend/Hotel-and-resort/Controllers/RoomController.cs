using Ganss.Xss;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Hotel_and_resort.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly AppDbContext _context;
        private readonly IRepository _repository;
        private readonly RoomService _roomService;
        private readonly ILogger<RoomController> _logger;
        private readonly IHtmlSanitizer _sanitizer;

        public RoomController(AppDbContext context, IRepository repository, RoomService roomService, ILogger<RoomController> logger)
        {
            _context = context;
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

        [HttpPost("cancel")]
        [Authorize(Roles = "Guest,Admin")]
        public async Task<IActionResult> CancelBooking([FromBody] BookingCancellationRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Declare userId at the beginning of the method to ensure it's accessible throughout
            string userId = null;

            try
            {
                // Sanitize inputs
                model.BookingId = int.Parse(_sanitizer.Sanitize(model.BookingId.ToString()));
                model.CancellationReason = _sanitizer.Sanitize(model.CancellationReason);

                // Get user context
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized cancellation attempt for booking {BookingId}", model.BookingId);
                    return Unauthorized(new { Error = "User not authenticated." });
                }

                // Fetch booking to verify ownership
                var booking = await _repository.GetBookingByIdAsync(model.BookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", model.BookingId);
                    return NotFound(new { Error = $"Booking with ID {model.BookingId} not found." });
                }

                if (booking.UserId != userId && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning("User {UserId} not authorized to cancel booking {BookingId}", userId, model.BookingId);
                    return Unauthorized(new { Error = "User not authorized to cancel this booking." });
                }

                // Get user email (assume CustomerService retrieves it)
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for user {UserId}", userId);
                    return BadRequest(new { Error = "Customer not found." });
                }

                // Cancel booking
                await _roomService.CancelBookingAsync(model.BookingId, model.CancellationReason, customer.Email, User.IsInRole("Admin"));

                return Ok(new { Message = "Booking cancelled successfully.", RefundPercentage = booking.RefundPercentage });
            }
            catch (BookingNotFoundException ex)
            {
                _logger.LogWarning(ex, "Booking not found: {BookingId}", model.BookingId);
                return NotFound(new { Error = ex.Message });
            }
            catch (RoomValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error for cancelling booking {BookingId}", model.BookingId);
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId} for user {UserId}", model.BookingId, userId);
                return StatusCode(500, new { Error = "Failed to cancel booking." });
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

        [Required]
        [Range(1, 10)]
        public int GuestCount { get; set; } = 1;
        public bool IsRefundable { get; set; }
    }


    public class BookingCancellationRequest
    {
        [Required]
        public int BookingId { get; set; }

        [MaxLength(500)]
        public string CancellationReason { get; set; }
    }
}