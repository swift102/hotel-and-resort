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
    public class BookingController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly ILogger<BookingController> _logger;

        public BookingController(IRepository repository, ILogger<BookingController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: api/bookings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingResponseDTO>>> GetBookings()
        {
            try
            {
                var bookings = await _repository.GetBookings();
                if (bookings == null || !bookings.Any())
                {
                    _logger.LogWarning("No bookings found.");
                    return NotFound();
                }

                // Map bookings to BookingResponseDTO
                var bookingDtos = bookings.Select(b => new BookingResponseDTO
                {
                    Id = b.Id,
                    RoomId = b.RoomId,
                    CustomerId = b.CustomerId,
                    CheckIn = b.CheckIn,
                    CheckOut = b.CheckOut,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status
                });

                return Ok(bookingDtos);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/bookings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BookingResponseDTO>> GetBooking(int id)
        {
            try
            {
                var booking = await _repository.GetBookingById(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found for ID: {Id}", id);
                    return NotFound();
                }

                var bookingDto = new BookingResponseDTO
                {
                    Id = booking.Id,
                    RoomId = booking.RoomId,
                    CustomerId = booking.CustomerId,
                    CheckIn = booking.CheckIn,
                    CheckOut = booking.CheckOut,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status
                };

                return Ok(bookingDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/bookings
        [HttpPost]
        public async Task<ActionResult<BookingResponseDTO>> AddBooking(BookingCreateDTO bookingDto)
        {
            try
            {
                if (bookingDto.CheckIn >= bookingDto.CheckOut)
                {
                    _logger.LogWarning("Check-in date must be before check-out date.");
                    return BadRequest("Check-in date must be before check-out date.");
                }

                var booking = new Booking
                {
                    RoomId = bookingDto.RoomId,
                    CustomerId = bookingDto.CustomerId,
                    CheckIn = bookingDto.CheckIn,
                    CheckOut = bookingDto.CheckOut,
                    TotalPrice = bookingDto.TotalPrice
                };

                var addedBooking = await _repository.AddBooking(booking);

                var addedBookingDto = new BookingResponseDTO
                {
                    Id = addedBooking.Id,
                    RoomId = addedBooking.RoomId,
                    CustomerId = addedBooking.CustomerId,
                    CheckIn = addedBooking.CheckIn,
                    CheckOut = addedBooking.CheckOut,
                    TotalPrice = addedBooking.TotalPrice,
                    Status = addedBooking.Status
                };

                return CreatedAtAction(nameof(GetBooking), new { id = addedBookingDto.Id }, addedBookingDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error adding booking");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/bookings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBooking(int id, BookingUpdateDTO bookingDto)
        {
            try
            {
                var booking = await _repository.GetBookingById(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found for ID: {Id}", id);
                    return NotFound();
                }

                // Update booking fields
                booking.CheckIn = bookingDto.CheckIn;
                booking.CheckOut = bookingDto.CheckOut;
                booking.TotalPrice = bookingDto.TotalPrice;
                booking.Status = bookingDto.Status;

                await _repository.UpdateBooking(booking);

                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error updating booking");
                return StatusCode(500, "Internal server error");
            }
        }


        // DELETE: api/bookings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            try
            {
                var booking = await _repository.GetBookingById(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found for ID: {Id}", id);
                    return NotFound();
                }

                await _repository.DeleteBooking(id);

                _logger.LogInformation("Booking deleted successfully: {BookingId}", id);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting booking");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
