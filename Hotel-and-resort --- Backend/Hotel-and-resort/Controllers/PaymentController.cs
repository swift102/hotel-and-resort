using hotel_and_resort.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using Hotel_and_resort.Services;
using System.ComponentModel.DataAnnotations;


namespace hotel_and_resort.Controllers
{

   

    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRepository _repository;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;


        public PaymentController(AppDbContext context, IRepository repository, ILogger<PaymentController> logger, IConfiguration configuration)
        {
            _context = context;
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("payfast/{bookingId}")]
        public async Task<IActionResult> InitiatePayFastPayment(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking {BookingId} not found", bookingId);
                    return NotFound(new { Error = "Booking not found." });
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    _logger.LogWarning("Booking {BookingId} is not in a payable state: CurrentStatus={Status}", bookingId, booking.Status);
                    return BadRequest(new { Error = "Booking is not in a payable state." });
                }

                var merchantId = _configuration["PayFast:MerchantId"] ?? "10038419"; // Use configuration
                var merchantKey = _configuration["PayFast:MerchantKey"] ?? "o6q83fm4xl05l"; // Use configuration
                var passphrase = _configuration["PayFast:Passphrase"] ?? "HOTELRESORTPAYFAST"; // Use configuration
                var returnUrl = _configuration["PayFast:ReturnUrl"] ?? "https://yourapp.com/payment-success";
                var cancelUrl = _configuration["PayFast:CancelUrl"] ?? "https://yourapp.com/payment-cancel";
                var notifyUrl = _configuration["PayFast:NotifyUrl"] ?? "https://yourapi.com/api/payment/notify";

                var data = new Dictionary<string, string>
                {
                    { "merchant_id", merchantId },
                    { "merchant_key", merchantKey },
                    { "return_url", returnUrl },
                    { "cancel_url", cancelUrl },
                    { "notify_url", notifyUrl },
                    { "amount", booking.TotalPrice.ToString("F2") },
                    { "item_name", $"Booking #{bookingId}" },
                    { "email_address", booking.Customer.Email },
                    { "m_payment_id", bookingId.ToString() }
                };

                var signature = GenerateSignature(data, passphrase);
                data.Add("signature", signature);

                var query = string.Join("&", data.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
                var redirectUrl = $"https://sandbox.payfast.co.za/eng/process?{query}";

                _logger.LogInformation("Initiated PayFast payment for Booking {BookingId}", bookingId);
                return Ok(new { RedirectUrl = redirectUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PayFast payment for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("notify")]
        public async Task<IActionResult> PayFastNotify()
        {
            try
            {
                // Read PayFast notification data
                var data = await new StreamReader(Request.Body).ReadToEndAsync();
                var notification = HttpUtility.ParseQueryString(data);

                // Validate signature
                var signature = notification["signature"];
                var passphrase = _configuration["PayFast:Passphrase"] ?? "HOTELRESORTPAYFAST"; // Use _configuration instead of builder.Configuration
                var expectedSignature = GenerateSignature(notification.AllKeys
                    .Where(k => k != "signature")
                    .ToDictionary(k => k, k => notification[k]), passphrase);
                if (signature != expectedSignature)
                {
                    _logger.LogWarning("Invalid PayFast signature for notification.");
                    return BadRequest(new { Error = "Invalid signature." });
                }

                var bookingId = int.Parse(notification["m_payment_id"]);
                var paymentStatus = notification["payment_status"];

                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking {BookingId} not found for PayFast notification", bookingId);
                    return NotFound(new { Error = "Booking not found." });
                }

                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = (int)(decimal.Parse(notification["amount_gross"]) * 100), // Convert to cents
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = "PayFast",
                    Status = paymentStatus == "COMPLETE" ? PaymentStatus.Completed : PaymentStatus.Failed
                };

                await _repository.AddPayment(payment);

                if (payment.Status == PaymentStatus.Completed)
                {
                    booking.Status = BookingStatus.Confirmed;
                    await _repository.UpdateRoomAvailability(booking.RoomId);
                    _logger.LogInformation("PayFast payment completed for Booking {BookingId}", bookingId);
                }
                else
                {
                    booking.Status = BookingStatus.Cancelled;
                    _logger.LogWarning("PayFast payment failed or cancelled for Booking {BookingId}", bookingId);
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayFast notification");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }


        private string GenerateSignature(Dictionary<string, string> data, string passphrase)
        {
            var sorted = data.OrderBy(x => x.Key).ToList();
            var sb = new StringBuilder();

            foreach (var kvp in sorted)
            {
                sb.Append($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}&");
            }

            if (!string.IsNullOrEmpty(passphrase))
                sb.Append("passphrase=" + HttpUtility.UrlEncode(passphrase));
            else
                sb.Length--;

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private string GenerateSignature(NameValueCollection data, string passphrase)
        {
            var sortedKeys = data.AllKeys.OrderBy(k => k).ToList();
            var sb = new StringBuilder();

            foreach (var key in sortedKeys)
            {
                if (key != "signature")
                {
                    sb.Append($"{key}={HttpUtility.UrlEncode(data[key])}&");
                }
            }

            if (!string.IsNullOrEmpty(passphrase))
                sb.Append("passphrase=" + HttpUtility.UrlEncode(passphrase));
            else
                sb.Length--;

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        [HttpPost("stripe/{bookingId}")]
        public async Task<IActionResult> ProcessStripePayment(int bookingId, [FromBody] StripePaymentDTO paymentDto)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking {BookingId} not found", bookingId);
                    return NotFound(new { Error = "Booking not found." });
                }

                var payment = await _repository.ProcessPaymentAndUpdateBooking(bookingId, (int)(booking.TotalPrice * 100), paymentDto.PaymentToken);
                if (payment.Status == PaymentStatus.Completed)
                {
                    _logger.LogInformation("Stripe payment completed for Booking {BookingId}", bookingId);
                    return Ok(new { Message = "Payment successful", PaymentId = payment.Id });
                }

                _logger.LogWarning("Stripe payment failed for Booking {BookingId}", bookingId);
                return BadRequest(new { Error = "Payment failed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe payment for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        public class StripePaymentDTO
        {
            [Required]
            public string PaymentToken { get; set; }
        }
    }
}