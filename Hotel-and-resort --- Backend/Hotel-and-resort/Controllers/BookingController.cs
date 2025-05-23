using Ganss.Xss;
using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static hotel_and_resort.ViewModels.AmenitiesDTOs;

namespace hotel_and_resort.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly CustomerService _customerService;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSenderService _smsSender;
        private readonly ILogger<BookingController> _logger;
        private readonly HtmlSanitizer _sanitizer;

        public BookingController(
            IRepository repository,
            CustomerService customerService,
            IEmailSender emailSender,
            ISmsSenderService smsSender,
            ILogger<BookingController> logger)
        {
            _repository = repository;
            _customerService = customerService;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
            _sanitizer = new HtmlSanitizer();
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Booking>> CreateBooking([FromBody] BookingCreateDTO bookingDto)
        {
            if (bookingDto == null)
            {
                _logger.LogWarning("Null booking data provided.");
                return BadRequest(new { Error = "Booking data cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid booking data provided.");
                return BadRequest(ModelState);
            }

            try
            {
                // Sanitize inputs
                bookingDto.CustomerFirstName = _sanitizer.Sanitize(bookingDto.CustomerFirstName);
                bookingDto.CustomerLastName = _sanitizer.Sanitize(bookingDto.CustomerLastName);
                bookingDto.CustomerEmail = _sanitizer.Sanitize(bookingDto.CustomerEmail);
                bookingDto.CustomerPhone = _sanitizer.Sanitize(bookingDto.CustomerPhone);

                // Validate dates
                if (bookingDto.CheckIn < DateTime.Today)
                {
                    _logger.LogWarning("Check-in date cannot be in the past.");
                    return BadRequest(new { Error = "Check-in date cannot be in the past." });
                }
                if (bookingDto.CheckOut <= bookingDto.CheckIn)
                {
                    _logger.LogWarning("Check-out date must be after check-in date.");
                    return BadRequest(new { Error = "Check-out date must be after check-in date." });
                }

                // Validate RoomId
                var roomExists = await _repository.RoomExistsAsync(bookingDto.RoomId);
                if (!roomExists)
                {
                    _logger.LogWarning("Room not found: {RoomId}", bookingDto.RoomId);
                    return NotFound(new { Error = $"Room with ID {bookingDto.RoomId} not found." });
                }

                // Get or create customer
                var customer = await _customerService.GetCustomerByEmailAsync(bookingDto.CustomerEmail);
                if (customer == null)
                {
                    customer = await _customerService.AddCustomerAsync(new Customer
                    {
                        FirstName = bookingDto.CustomerFirstName,
                        LastName = bookingDto.CustomerLastName,
                        Email = bookingDto.CustomerEmail,
                        Phone = bookingDto.CustomerPhone
                    });
                }

                // Check user ownership
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!User.IsInRole("Admin") && customer.Id.ToString() != userId)
                {
                    _logger.LogWarning("Unauthorized booking attempt by user {UserId} for customer {CustomerId}", userId, customer.Id);
                    return Forbid();
                }

                // Begin transaction
                using var transaction = await _repository.BeginTransactionAsync();
                try
                {
                    // Check room availability
                    var isAvailable = await _repository.IsRoomAvailableAsync(bookingDto.RoomId, bookingDto.CheckIn, bookingDto.CheckOut);
                    if (!isAvailable)
                    {
                        _logger.LogWarning("Room {RoomId} is not available for dates {CheckIn} to {CheckOut}",
                            bookingDto.RoomId, bookingDto.CheckIn, bookingDto.CheckOut);
                        return Conflict(new { Error = "Room is not available for the selected dates." });
                    }

                    var booking = new Booking
                    {
                        RoomId = bookingDto.RoomId,
                        //CustomerId = customer.Id,
                        CheckIn = bookingDto.CheckIn,
                        CheckOut = bookingDto.CheckOut,
                        Status = BookingStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    var addedBooking = await _repository.AddBookingAsync(booking);
                    await transaction.CommitAsync();

                    // Publish event (for extensibility)
                    await PublishBookingCreatedEvent(addedBooking);

                    // Send notifications
                    await SendBookingConfirmation(customer, addedBooking);

                    _logger.LogInformation("Booking created: {BookingId} for customer {CustomerId}", addedBooking.Id, customer.Id);
                    return CreatedAtAction(nameof(GetBooking), new { id = addedBooking.Id }, addedBooking);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (CustomerValidationException ex)
            {
                _logger.LogWarning("Invalid customer data: {Message}", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
            catch (DuplicateCustomerException ex)
            {
                _logger.LogWarning("Duplicate customer error: {Message}", ex.Message);
                return Conflict(new { Error = ex.Message });
            }
            catch (CustomerServiceException ex)
            {
                _logger.LogError(ex, "Error creating customer for booking with email {Email}", bookingDto.CustomerEmail);
                return StatusCode(500, new { Error = ex.Message });
            }
            catch (BookingConflictException ex)
            {
                _logger.LogWarning("Booking conflict: {Message}", ex.Message);
                return Conflict(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking for room {RoomId}", bookingDto.RoomId);
                return StatusCode(500, new { Error = "Failed to create booking." });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Booking>> GetBooking(int id)
        {
            try
            {
                var booking = await _repository.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", id);
                    return NotFound(new { Error = $"Booking with ID {id} not found." });
                }

                // Restrict to admin or booking owner
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!User.IsInRole("Admin") && booking.CustomerId.ToString() != userId)
                {
                    _logger.LogWarning("Unauthorized access to booking {BookingId} by user {UserId}", id, userId);
                    return Forbid();
                }

                return Ok(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking with ID {BookingId}", id);
                return StatusCode(500, new { Error = "Failed to retrieve booking." });
            }
        }

        [HttpGet("admin/bookings")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetAllBookings(int page = 1, int pageSize = 10)
        {
            try
            {
                var bookings = await _repository.GetAllBookingsAsync(page, pageSize);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings, page {Page}, pageSize {PageSize}", page, pageSize);
                return StatusCode(500, new { Error = "Failed to retrieve bookings." });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] BookingStatusDTO statusDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid booking status data provided.");
                return BadRequest(ModelState);
            }

            try
            {
                var booking = await _repository.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", id);
                    return NotFound(new { Error = $"Booking with ID {id} not found." });
                }

                booking.Status = statusDto.Status;
                await _repository.UpdateBookingAsync(booking);
                _logger.LogInformation("Booking status updated: {BookingId} to {Status}", id, statusDto.Status);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking status for ID {BookingId}", id);
                return StatusCode(500, new { Error = "Failed to update booking status." });
            }
        }

        private async Task PublishBookingCreatedEvent(Booking booking)
        {
            // Placeholder for event publishing (e.g., via MediatR or event bus)
            _logger.LogInformation("Published BookingCreatedEvent for booking {BookingId}", booking.Id);
            // Future: Publish to message queue or event handler
        }

        private async Task SendBookingConfirmation(Customer customer, Booking booking)
        {
            try
            {
                var room = await _repository.GetRoomByIdAsync(booking.RoomId);
                if (room == null)
                {
                    _logger.LogError("Room not found for booking {BookingId}", booking.Id);
                    return;
                }

                await _emailSender.SendBookingConfirmationEmailAsync(customer.Email, booking, room);
                var smsMessage = $"Booking {booking.Id} confirmed for {booking.CheckIn:dd-MM-yyyy} to {booking.CheckOut:dd-MM-yyyy}.";
                await _smsSender.SendSmsAsync(customer.Phone, smsMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending booking confirmation to customer {CustomerId}", customer.Id);
            }
        }
    }

    public class BookingCreateDTO
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public DateTime CheckIn { get; set; }

        [Required]
        public DateTime CheckOut { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerFirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerLastName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string CustomerEmail { get; set; }

        [Required]
        [Phone]
        [MaxLength(20)]
        public string CustomerPhone { get; set; }
    }

    public class BookingStatusDTO
    {
        [Required]
        public BookingStatus Status { get; set; }
    }

    public class BookingConflictException : Exception
    {
        public BookingConflictException(string message) : base(message) { }
        public BookingConflictException(string message, Exception innerException) : base(message, innerException) { }
    }
}