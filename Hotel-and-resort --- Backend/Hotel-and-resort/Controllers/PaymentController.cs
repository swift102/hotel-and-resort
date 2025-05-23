using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using hotel_and_resort.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;


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
        private readonly HttpClient _httpClient;
        private readonly PaymentService _paymentService;


        public PaymentController(AppDbContext context, IRepository repository, PaymentService paymentService, ILogger<PaymentController> logger, IConfiguration configuration)
        {
            _context = context;
            _repository = repository;
            _paymentService = paymentService;
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

                var success = await _paymentService.InitiatePayFastPaymentAsync(bookingId, booking.TotalPrice, booking.Customer.Email);
                if (!success)
                {
                    _logger.LogWarning("Failed to initiate PayFast payment for Booking {BookingId}", bookingId);
                    return BadRequest(new { Error = "Failed to initiate payment." });
                }

                var merchantId = _configuration["PayFast:MerchantId"] ?? "10000100";
                var merchantKey = _configuration["PayFast:MerchantKey"] ?? "46f0cd694581a";
                var passphrase = _configuration["PayFast:Passphrase"] ?? "test_passphrase";
                var returnUrl = _configuration["PayFast:ReturnUrl"] ?? "http://localhost:4200/payment-success";
                var cancelUrl = _configuration["PayFast:CancelUrl"] ?? "http://localhost:4200/payment-cancel";
                var notifyUrl = _configuration["PayFast:NotifyUrl"] ?? "http://localhost:5000/api/Payment/notify";

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
                var data = await new StreamReader(Request.Body).ReadToEndAsync();
                var notification = HttpUtility.ParseQueryString(data);

                var signature = notification["signature"];
                var passphrase = _configuration["PayFast:Passphrase"] ?? "test_passphrase";
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
                    await _repository.UpdateRoomAvailabilityAsync(booking.RoomId);
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

                var clientSecret = await _paymentService.CreatePaymentIntentAsync((int)(booking.TotalPrice * 100)); // Convert to cents
                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = (int)(booking.TotalPrice * 100),
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = "Stripe",
                    Status = PaymentStatus.Completed, // Assume success for demo; in production, verify with Stripe
                    StripePaymentIntentId = clientSecret // Store for refunds
                };

                await _repository.AddPayment(payment);
                booking.Status = BookingStatus.Confirmed;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stripe payment completed for Booking {BookingId}", bookingId);
                return Ok(new { Message = "Payment successful", PaymentId = payment.Id, ClientSecret = clientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe payment for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("stripe/checkout/{bookingId}")]
        public async Task<IActionResult> CreateStripeCheckoutSession(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking {BookingId} not found", bookingId);
                    return NotFound(new { Error = "Booking not found." });
                }

                var session = await _paymentService.CreateCheckoutSessionAsync((int)(booking.TotalPrice * 100));
                _logger.LogInformation("Stripe Checkout Session created for Booking {BookingId}: {SessionId}", bookingId, session.Id);
                return Ok(new { SessionId = session.Id, Url = session.Url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe Checkout Session for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("refund/{paymentId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> InitiateRefund(int paymentId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Booking)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                    return NotFound(new { Error = "Payment not found." });
                }

                if (payment.Status != PaymentStatus.Completed)
                {
                    _logger.LogWarning("Payment {PaymentId} is not eligible for refund", paymentId);
                    return BadRequest(new { Error = "Payment is not eligible for refund." });
                }

                bool success;
                if (payment.PaymentMethod == "PayFast")
                {
                    success = await _paymentService.InitiatePayFastRefundAsync(paymentId, payment.Amount / 100m, payment.BookingId);
                }
                else if (payment.PaymentMethod == "Stripe")
                {
                    success = await _paymentService.InitiateStripeRefundAsync(payment.StripePaymentIntentId);
                }
                else
                {
                    _logger.LogWarning("Unsupported payment method for refund: {PaymentMethod}", payment.PaymentMethod);
                    return BadRequest(new { Error = "Unsupported payment method." });
                }

                if (success)
                {
                    payment.Status = PaymentStatus.Refunded;
                    payment.Booking.Status = BookingStatus.Refunded;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Refund initiated for Payment {PaymentId}", paymentId);
                    return Ok(new { Message = "Refund processed successfully." });
                }

                _logger.LogWarning("Refund failed for Payment {PaymentId}", paymentId);
                return BadRequest(new { Error = "Refund processing failed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating refund for Payment {PaymentId}", paymentId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        private string GenerateSignature(Dictionary<string, string> data, string passphrase)
        {
            var sorted = data.OrderBy(x => x.Key);
            var sb = new StringBuilder();
            foreach (var kvp in sorted)
            {
                sb.Append($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}&");
            }
            if (!string.IsNullOrEmpty(passphrase))
                sb.Append($"passphrase={HttpUtility.UrlEncode(passphrase)}");
            else
                sb.Length--;

            using var md5 = System.Security.Cryptography.MD5.Create();
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
                sb.Append($"passphrase={HttpUtility.UrlEncode(passphrase)}");
            else
                sb.Length--;

            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public class StripePaymentDTO
        {
            public string PaymentToken { get; set; }
        }
    }
}