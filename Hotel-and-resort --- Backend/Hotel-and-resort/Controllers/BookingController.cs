using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace hotel_and_resort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PaymentService _paymentService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(AppDbContext context, PaymentService paymentService, ILogger<BookingController> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<BookingResponseDTO>> AddBooking([FromBody] BookingCreateDTO bookingDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var booking = new Booking
                {
                    RoomId = bookingDto.RoomId,
                    CustomerId = bookingDto.CustomerId,
                    CheckIn = bookingDto.CheckIn,
                    CheckOut = bookingDto.CheckOut,
                    TotalPrice = bookingDto.TotalPrice,
                    Status = BookingStatus.Pending
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                var addedBookingDto = new BookingResponseDTO
                {
                    Id = booking.Id,
                    RoomId = booking.RoomId,
                    CustomerId = booking.CustomerId,
                    CheckIn = booking.CheckIn,
                    CheckOut = booking.CheckOut,
                    TotalPrice = (int)booking.TotalPrice,
                    Status = booking.Status.ToString()
                };

                return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, addedBookingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding booking");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookingResponseDTO>> GetBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            var bookingDto = new BookingResponseDTO
            {
                Id = booking.Id,
                RoomId = booking.RoomId,
                CustomerId = booking.CustomerId,
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                TotalPrice = (int)booking.TotalPrice,
                Status = booking.Status.ToString()
            };

            return Ok(bookingDto);
        }

        [HttpPost("{id}/process-payment")]
        public async Task<IActionResult> ProcessPayment(int id, [FromBody] ProcessPaymentDto paymentDto)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null) return NotFound();

                var clientSecret = await _paymentService.CreatePaymentIntentAsync(paymentDto.Amount, paymentDto.Currency);
                // Assume frontend confirms the payment and calls back; update status here for demo
                booking.Status = BookingStatus.Confirmed;
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Payment successful", ClientSecret = clientSecret });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error processing payment for booking {BookingId}", id);
                return BadRequest(new { Message = "Payment failed", Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for booking {BookingId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class BookingCreateDTO
    {
        [Required]
        public int RoomId { get; set; }
        [Required]
        public int CustomerId { get; set; }
        [Required]
        public DateTime CheckIn { get; set; }
        [Required]
        public DateTime CheckOut { get; set; }
        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }
    }

    public class ProcessPaymentDto
    {
        [Required, Range(1, int.MaxValue)]
        public int Amount { get; set; }
        [Required]
        public string Currency { get; set; } = "usd";
    }
}