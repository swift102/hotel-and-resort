using Ganss.Xss;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
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
        private readonly IRepository _repository;
        private readonly RoomService _roomService;
        private readonly PaymentService _paymentService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHtmlSanitizer _sanitizer;

        public PaymentController(
            IRepository repository,
            RoomService roomService,
            PaymentService paymentService,
            IEmailSender emailSender,
            ILogger<PaymentController> logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _roomService = roomService;
            _paymentService = paymentService;
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
            _sanitizer = new HtmlSanitizer();
        }

        [HttpPost("payfast/{bookingId}")]
        [Authorize(Roles = "Guest,Admin")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> InitiatePayFastPayment(int bookingId)
        {
            try
            {
                // Sanitize input
                bookingId = int.Parse(_sanitizer.Sanitize(bookingId.ToString()));

                // Validate user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized payment attempt");
                    return Unauthorized(new { Error = "User not authenticated." });
                }

                var userProfileId = User.FindFirst("UserProfileID")?.Value;
                if (string.IsNullOrEmpty(userProfileId) || !int.TryParse(userProfileId, out int profileId))
                {
                    _logger.LogWarning("UserProfileID not found or invalid for user {UserId}", userId);
                    return BadRequest(new { Error = "User profile not configured." });
                }

                // Retrieve booking
                var booking = await _repository.GetBookingByIdAsync(bookingId);
                if (booking == null || booking.UserId != userId || booking.UserProfileID != profileId)
                {
                    _logger.LogWarning("Booking {BookingId} not found or unauthorized for user {UserId}", bookingId, userId);
                    return NotFound(new { Error = "Booking not found or unauthorized." });
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    _logger.LogWarning("Booking {BookingId} is not in a payable state: CurrentStatus={Status}", bookingId, booking.Status);
                    return BadRequest(new { Error = "Booking is not in a payable state." });
                }

                // Verify room availability
                var isAvailable = await _roomService.IsRoomAvailableAsync(booking.RoomId, booking.CheckIn, booking.CheckOut);
                if (!isAvailable)
                {
                    _logger.LogWarning("Room {RoomId} not available for booking {BookingId}", booking.RoomId, bookingId);
                    return BadRequest(new { Error = "Room is no longer available." });
                }

                // Get customer data (placeholder; replace with CustomerService)
                var customer = new Customer { Id = booking.UserProfileID, Email = "customer@example.com" };
                var success = await _paymentService.InitiatePayFastPaymentAsync(bookingId, booking.TotalPrice, customer.Email);
                if (!success)
                {
                    _logger.LogWarning("Failed to initiate PayFast payment for Booking {BookingId}", bookingId);
                    return BadRequest(new { Error = "Failed to initiate payment." });
                }

                // Generate PayFast redirect URL
                var merchantId = _configuration["PayFast:MerchantId"] ?? "10000100";
                var merchantKey = _configuration["PayFast:MerchantKey"] ?? "46f0cd694581a";
                var passphrase = _configuration["PayFast:Passphrase"] ?? "test_passphrase";
                var returnUrl = _configuration["PayFast:ReturnUrl"] ?? "http://localhost:4200/payment-success";
                var cancelUrl = _configuration["PayFast:CancelUrl"] ?? "http://localhost:4200/payment-cancel";
                var notifyUrl = _configuration["PayFast:NotifyUrl"] ?? "https://your-api-domain.com/api/Payment/notify";

                var data = new Dictionary<string, string>
                {
                    { "merchant_id", merchantId },
                    { "merchant_key", merchantKey },
                    { "return_url", returnUrl },
                    { "cancel_url", cancelUrl },
                    { "notify_url", notifyUrl },
                    { "amount", booking.TotalPrice.ToString("F2") },
                    { "item_name", $"Booking #{bookingId}" },
                    { "email_address", customer.Email },
                    { "m_payment_id", bookingId.ToString() }
                };

                var signature = _paymentService.GenerateSignature(data, passphrase);
                data.Add("signature", signature);

                var query = string.Join("&", data.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
                var redirectUrl = $"https://sandbox.payfast.co.za/eng/process?{query}";

                _logger.LogInformation("Initiated PayFast payment for Booking {BookingId}", bookingId);
                return Ok(new { RedirectUrl = redirectUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PayFast payment for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = "Failed to initiate payment." });
            }
        }

        [HttpPost("notify")]
        public async Task<IActionResult> PayFastNotify()
        {
            try
            {
                // Validate PayFast IP (simplified; use PayFast's official IP list in production)
                var remoteIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                if (remoteIp != "196.33.227.0" && remoteIp != "127.0.0.1") // Example IPs
                {
                    _logger.LogWarning("Unauthorized PayFast notification from IP {RemoteIp}", remoteIp);
                    return Unauthorized(new { Error = "Invalid source IP." });
                }

                var data = await new StreamReader(Request.Body).ReadToEndAsync();
                var notification = HttpUtility.ParseQueryString(data);

                var signature = notification["signature"];
                var passphrase = _configuration["PayFast:Passphrase"] ?? "test_passphrase";
                var expectedSignature = _paymentService.GenerateSignature(notification.AllKeys
                    .Where(k => k != "signature")
                    .ToDictionary(k => k, k => notification[k]), passphrase);
                if (signature != expectedSignature)
                {
                    _logger.LogWarning("Invalid PayFast signature for notification.");
                    return BadRequest(new { Error = "Invalid signature." });
                }

                var bookingId = int.Parse(notification["m_payment_id"]);
                var paymentStatus = notification["payment_status"];

                var booking = await _repository.GetBookingByIdAsync(bookingId);
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

                await _repository.AddPaymentAsync(payment);

                if (payment.Status == PaymentStatus.Completed)
                {
                    booking.Status = BookingStatus.Confirmed;
                    await _repository.UpdateRoomAvailabilityAsync(booking.RoomId);

                    // Send payment confirmation email
                    var customer = new Customer { Id = booking.UserProfileID, Email = "customer@example.com" }; // Placeholder
                    var room = await _repository.GetRoomByIdAsync(booking.RoomId);
                    if (room != null)
                    {
                        await _emailSender.SendPaymentConfirmationEmailAsync(customer.Email, booking, room, $"PF-{bookingId}");
                    }

                    _logger.LogInformation("PayFast payment completed for Booking {BookingId}", bookingId);
                }
                else
                {
                    booking.Status = BookingStatus.Cancelled;
                    _logger.LogWarning("PayFast payment failed or cancelled for Booking {BookingId}", bookingId);
                }

                await _repository.UpdateBookingAsync(booking);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayFast notification");
                return StatusCode(500, new { Error = "Failed to process notification." });
            }
        }

        [HttpPost("stripe/{bookingId}")]
        [Authorize(Roles = "Guest,Admin")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ProcessStripePayment(int bookingId, [FromBody] StripePaymentDTO paymentDto)
        {
            try
            {
                // Sanitize input
                bookingId = int.Parse(_sanitizer.Sanitize(bookingId.ToString()));
                paymentDto.PaymentToken = _sanitizer.Sanitize(paymentDto.PaymentToken);

                // Validate user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized payment attempt");
                    return Unauthorized(new { Error = "User not authenticated." });
                }

                var userProfileId = User.FindFirst("UserProfileID")?.Value;
                if (string.IsNullOrEmpty(userProfileId) || !int.TryParse(userProfileId, out int profileId))
                {
                    _logger.LogWarning("UserProfileID not found or invalid for user {UserId}", userId);
                    return BadRequest(new { Error = "User profile not configured." });
                }

                // Retrieve booking
                var booking = await _repository.GetBookingByIdAsync(bookingId);
                if (booking == null || booking.UserId != userId || booking.UserProfileID != profileId)
                {
                    _logger.LogWarning("Booking {BookingId} not found or unauthorized for user {UserId}", bookingId, userId);
                    return NotFound(new { Error = "Booking not found or unauthorized." });
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    _logger.LogWarning("Booking {BookingId} is not in a payable state: CurrentStatus={Status}", bookingId, booking.Status);
                    return BadRequest(new { Error = "Booking is not in a payable state." });
                }

                // Verify room availability
                var isAvailable = await _roomService.IsRoomAvailableAsync(booking.RoomId, booking.CheckIn, booking.CheckOut);
                if (!isAvailable)
                {
                    _logger.LogWarning("Room {RoomId} not available for booking {BookingId}", booking.RoomId, bookingId);
                    return BadRequest(new { Error = "Room is no longer available." });
                }

                // Create payment intent
                var amountInCents = (int)(booking.TotalPrice * 100);
                var clientSecret = await _paymentService.CreatePaymentIntentAsync(amountInCents);
                var paymentIntentId = clientSecret; // Simplified; in production, retrieve actual ID

                // Verify payment (simplified; in production, confirm with Stripe API)
                var paymentIntentService = new Stripe.PaymentIntentService();
                var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
                if (paymentIntent.Status != "succeeded")
                {
                    _logger.LogWarning("Stripe payment not succeeded for Booking {BookingId}: Status={Status}", bookingId, paymentIntent.Status);
                    return BadRequest(new { Error = "Payment not completed." });
                }

                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = amountInCents,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = "Stripe",
                    Status = PaymentStatus.Completed,
                    StripePaymentIntentId = paymentIntentId
                };

                await _repository.AddPaymentAsync(payment);
                booking.Status = BookingStatus.Confirmed;
                await _repository.UpdateBookingAsync(booking);
                await _repository.UpdateRoomAvailabilityAsync(booking.RoomId);

                // Send payment confirmation email
                var customer = new Customer { Id = booking.UserProfileID, Email = "customer@example.com" }; // Placeholder
                var room = await _repository.GetRoomByIdAsync(booking.RoomId);
                if (room != null)
                {
                    await _emailSender.SendPaymentConfirmationEmailAsync(customer.Email, booking, room, paymentIntentId);
                }

                _logger.LogInformation("Stripe payment completed for Booking {BookingId}", bookingId);
                return Ok(new { Message = "Payment successful", PaymentId = payment.Id, ClientSecret = clientSecret });
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex, "Stripe error processing payment for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = $"Payment error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe payment for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = "Failed to process payment." });
            }
        }

        [HttpPost("stripe/checkout/{bookingId}")]
        [Authorize(Roles = "Guest,Admin")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> CreateStripeCheckoutSession(int bookingId)
        {
            try
            {
                // Sanitize input
                bookingId = int.Parse(_sanitizer.Sanitize(bookingId.ToString()));

                // Validate user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized payment attempt");
                    return Unauthorized(new { Error = "User not authenticated." });
                }

                var userProfileId = User.FindFirst("UserProfileID")?.Value;
                if (string.IsNullOrEmpty(userProfileId) || !int.TryParse(userProfileId, out int profileId))
                {
                    _logger.LogWarning("UserProfileID not found or invalid for user {UserId}", userId);
                    return BadRequest(new { Error = "User profile not configured." });
                }

                // Retrieve booking
                var booking = await _repository.GetBookingByIdAsync(bookingId);
                if (booking == null || booking.UserId != userId || booking.UserProfileID != profileId)
                {
                    _logger.LogWarning("Booking {BookingId} not found or unauthorized for user {UserId}", bookingId, userId);
                    return NotFound(new { Error = "Booking not found or unauthorized." });
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    _logger.LogWarning("Booking {BookingId} is not in a payable state: CurrentStatus={Status}", bookingId, booking.Status);
                    return BadRequest(new { Error = "Booking is not in a payable state." });
                }

                // Verify room availability
                var isAvailable = await _roomService.IsRoomAvailableAsync(booking.RoomId, booking.CheckIn, booking.CheckOut);
                if (!isAvailable)
                {
                    _logger.LogWarning("Room {RoomId} not available for booking {BookingId}", booking.RoomId, bookingId);
                    return BadRequest(new { Error = "Room is no longer available." });
                }

                // Create checkout session
                var amountInCents = (int)(booking.TotalPrice * 100);
                var session = await _paymentService.CreateCheckoutSessionAsync(amountInCents, bookingId);
                _logger.LogInformation("Stripe Checkout Session created for Booking {BookingId}: {SessionId}", bookingId, session.Id);

                // Update booking with session ID
                booking.PaymentIntentId = session.PaymentIntentId;
                await _repository.UpdateBookingAsync(booking);

                return Ok(new { SessionId = session.Id, Url = session.Url });
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating Checkout Session for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = $"Checkout error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe Checkout Session for Booking {BookingId}", bookingId);
                return StatusCode(500, new { Error = "Failed to create checkout session." });
            }
        }

        [HttpPost("refund/{paymentId}")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> InitiateRefund(int paymentId)
        {
            try
            {
                // Sanitize input
                paymentId = int.Parse(_sanitizer.Sanitize(paymentId.ToString()));

                var payment = await _repository.GetPaymentByIdAsync(paymentId);
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
                    var booking = await _repository.GetBookingByIdAsync(payment.BookingId);
                    if (booking != null)
                    {
                        booking.Status = BookingStatus.Refunded;
                        await _repository.UpdateBookingAsync(booking);
                        await _repository.UpdateRoomAvailabilityAsync(booking.RoomId);
                    }

                    await _repository.UpdatePaymentAsync(payment);
                    _logger.LogInformation("Refund initiated for Payment {PaymentId}", paymentId);
                    return Ok(new { Message = "Refund processed successfully." });
                }

                _logger.LogWarning("Refund failed for Payment {PaymentId}", paymentId);
                return BadRequest(new { Error = "Refund processing failed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating refund for Payment {PaymentId}", paymentId);
                return StatusCode(500, new { Error = "Failed to process refund." });
            }
        }
    }

    public class StripePaymentDTO
    {
        [Required]
        public string PaymentToken { get; set; }
    }
}