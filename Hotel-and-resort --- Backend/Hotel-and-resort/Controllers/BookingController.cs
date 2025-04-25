using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static hotel_and_resort.ViewModels.AmenitiesDTOs;

namespace hotel_and_resort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRepository _repository;
        private readonly ILogger<BookingController> _logger;

        public BookingController(AppDbContext context, IRepository repository, ILogger<BookingController> logger)
        {
            _context = context;
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("available-rooms")]
        public async Task<ActionResult<IEnumerable<RoomReadDTO>>> GetAvailableRooms([FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
        {
            if (checkIn < DateTime.Today || checkOut <= checkIn)
            {
                _logger.LogWarning("Invalid date range: CheckIn={CheckIn}, CheckOut={CheckOut}", checkIn, checkOut);
                return BadRequest(new { Error = "Invalid date range. Check-in must be today or later, and check-out must be after check-in." });
            }

            try
            {
                var bookedRoomIds = await _context.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled &&
                                (checkIn <= b.CheckOut && checkOut >= b.CheckIn))
                    .Select(b => b.RoomId)
                    .ToListAsync();

                var availableRooms = await _context.Rooms
                    .Where(r => !bookedRoomIds.Contains(r.Id) && r.IsAvailable)
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .ToListAsync();

                var roomDtos = availableRooms.Select(r => new RoomReadDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Price = r.Price,
                    Capacity = r.Capacity,
                    Features = r.Features,
                    IsAvailable = r.IsAvailable,
                    Amenities = r.Amenities?.Select(a => new AmenityListDTO
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Description = a.Description
                    }).ToList(),
                    Images = r.Images?.Select(i => new ImageReadDTO
                    {
                        Id = i.Id,
                        Name = i.Name,
                        ImagePath = i.ImagePath,
                        RoomID = i.RoomID
                    }).ToList()
                }).ToList();

                if (!roomDtos.Any())
                {
                    _logger.LogWarning("No available rooms found for dates {CheckIn} to {CheckOut}", checkIn, checkOut);
                    return NotFound(new { Error = "No available rooms for the selected dates." });
                }

                _logger.LogInformation("Found {RoomCount} available rooms for dates {CheckIn} to {CheckOut}", roomDtos.Count, checkIn, checkOut);
                return Ok(roomDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available rooms for dates {CheckIn} to {CheckOut}", checkIn, checkOut);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Booking>> CreateBooking([FromBody] BookingCreateDTO bookingDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid booking data received.");
                return BadRequest(ModelState);
            }

            try
            {
                // Verify room availability
                var isAvailable = await _repository.IsRoomAvailable(bookingDto.RoomId, bookingDto.CheckIn, bookingDto.CheckOut);
                if (!isAvailable)
                {
                    _logger.LogWarning("Room {RoomId} is not available for dates {CheckIn} to {CheckOut}", bookingDto.RoomId, bookingDto.CheckIn, bookingDto.CheckOut);
                    return BadRequest(new { Error = "The selected room is not available for the specified dates." });
                }

                // Fetch or create customer
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == bookingDto.CustomerEmail);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        FirstName = bookingDto.CustomerFirstName,
                        LastName = bookingDto.CustomerLastName,
                        Email = bookingDto.CustomerEmail,
                        Phone = bookingDto.CustomerPhone,
                        Title = bookingDto.CustomerTitle
                    };
                    await _repository.AddCustomer(customer);
                    _logger.LogInformation("Created new customer with email {Email}", customer.Email);
                }

                // Calculate total price
                var room = await _context.Rooms.FindAsync(bookingDto.RoomId);
                if (room == null)
                {
                    _logger.LogWarning("Room {RoomId} not found", bookingDto.RoomId);
                    return NotFound(new { Error = "Room not found." });
                }

                var nights = (bookingDto.CheckOut - bookingDto.CheckIn).Days;
                if (nights <= 0)
                {
                    _logger.LogWarning("Invalid booking duration: {Nights} nights", nights);
                    return BadRequest(new { Error = "Check-out date must be after check-in date." });
                }
                var totalPrice = room.Price * nights;

                var booking = new Booking
                {
                    RoomId = bookingDto.RoomId,
                    CustomerId = customer.Id,
                    CheckIn = bookingDto.CheckIn,
                    CheckOut = bookingDto.CheckOut,
                    TotalPrice = totalPrice,
                    Status = BookingStatus.Pending,
                    IsRefundable = bookingDto.IsRefundable
                };

                var addedBooking = await _repository.AddBooking(booking);
                _logger.LogInformation("Booking created successfully: BookingId={BookingId}, RoomId={RoomId}", addedBooking.Id, addedBooking.RoomId);

                return CreatedAtAction(nameof(GetBooking), new { id = addedBooking.Id }, addedBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking for RoomId={RoomId}", bookingDto.RoomId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Booking>> GetBooking(int id)
        {
            try
            {
                var booking = await _repository.GetBookingById(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking {BookingId} not found", id);
                    return NotFound(new { Error = "Booking not found." });
                }
                return Ok(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking {BookingId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }


        [HttpGet("admin/bookings")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetAllBookings()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .Include(b => b.Payments)
                    .ToListAsync();
                _logger.LogInformation("Admin retrieved {BookingCount} bookings", bookings.Count);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all bookings for admin");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPut("admin/bookings/{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] BookingStatusDTO statusDto)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking {BookingId} not found for status update", id);
                    return NotFound(new { Error = "Booking not found." });
                }

                booking.Status = statusDto.Status;
                if (statusDto.Status == BookingStatus.Cancelled && booking.IsRefundable)
                {
                    var payment = await _context.Payments
                        .Where(p => p.BookingId == id && p.Status == PaymentStatus.Completed)
                        .FirstOrDefaultAsync();
                    if (payment != null)
                    {
                        payment.Status = PaymentStatus.Refunded;
                        // TODO: Initiate refund via PayFast API (requires PayFast refund API integration)
                        _logger.LogInformation("Marked payment {PaymentId} as refunded for Booking {BookingId}", payment.Id, id);
                    }
                }

                await _context.SaveChangesAsync();
                await _repository.UpdateRoomAvailability(booking.RoomId);
                _logger.LogInformation("Admin updated Booking {BookingId} status to {Status}", id, statusDto.Status);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Booking {BookingId} status", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

     
    }

   
}