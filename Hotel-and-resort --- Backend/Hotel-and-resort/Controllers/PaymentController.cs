using Ganss.Xss;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Hotel_and_resort.Data;
using Hotel_and_resort.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
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
        private readonly ICustomerService _customerService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHtmlSanitizer _sanitizer;
        private readonly AppDbContext _context;

        public PaymentController(
            IRepository repository,
            RoomService roomService,
            PaymentService paymentService,
            IEmailSender emailSender,
            ICustomerService customerService,
            ILogger<PaymentController> logger,
            IConfiguration configuration,
            AppDbContext context)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _sanitizer = new HtmlSanitizer();
            _context = context ?? throw new ArgumentNullException(nameof(context));
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

                // Get customer
                var customer = await _customerService.GetCustomerByIdAsync(booking.UserProfileID);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for UserProfileID {UserProfileID}", booking.UserProfileID);
                    return BadRequest(new { Error = "Customer not found." });
                }

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
                var notifyUrl = _configuration["PayFast:NotifyUrl"] ?? "https://your-api-domain.com/api/payment/notify";

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
                var allowedIps = _configuration.GetSection("PayFast:AllowedIps").Get<string[]>(); 
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var remoteIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                if (!allowedIps.Contains(clientIp))
                {
                    _logger.LogWarning("Invalid PayFast notification IP: {ClientIp}", clientIp);
                    return BadRequest(new ErrorResponse { Message = "Invalid notification source." });
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

                var customer = await _customerService.GetCustomerByIdAsync(booking.UserProfileID);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for UserProfileID {UserProfileID}", booking.UserProfileID);
                    return BadRequest(new { Error = "Customer not found." });
                }

                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = decimal.Parse(notification["amount_gross"]), // PayFast returns in ZAR
                    Currency = "ZAR",
                    PaymentMethod = "PayFast",
                    TransactionId = notification["pf_payment_id"],
                    Status = paymentStatus == "COMPLETE" ? PaymentStatus.Succeeded : PaymentStatus.Failed,
                    CustomerId = booking.UserProfileID,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Payments.AddAsync(payment);

                if (payment.Status == PaymentStatus.Succeeded)
                {
                    booking.Status = BookingStatus.Confirmed;
                    _context.Bookings.Update(booking);

                    // Send payment confirmation email
                    var room = await _context.Rooms.FindAsync(booking.RoomId);
                    if (room != null)
                    {
                        var emailBody = _sanitizer.Sanitize(
                            $"<h3>Payment Confirmation</h3><p>Your payment for Booking #{bookingId} ({room.Name}) has been received.</p>");
                        await _emailSender.SendEmailAsync(customer.Email, "Payment Confirmation", emailBody);
                    }

                    _logger.LogInformation("PayFast payment completed for Booking {BookingId}, Payment {PaymentId}", bookingId, payment.Id);
                }
                else
                {
                    booking.Status = BookingStatus.Cancelled;
                    _context.Bookings.Update(booking);
                    _logger.LogWarning("PayFast payment failed or cancelled for Booking {BookingId}", bookingId);
                }

                await _context.SaveChangesAsync();
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
                var paymentIntentId = await _paymentService.CreatePaymentIntentAsync(amountInCents, bookingId.ToString());

                // Confirm payment intent (simplified; in production, client confirms)
                var paymentIntentService = new Stripe.PaymentIntentService();
                var confirmOptions = new PaymentIntentConfirmOptions { PaymentMethod = paymentDto.PaymentToken };
                var paymentIntent = await paymentIntentService.ConfirmAsync(paymentIntentId, confirmOptions);
                if (paymentIntent.Status != "succeeded")
                {
                    _logger.LogWarning("Stripe payment not succeeded for Booking {BookingId}: Status={Status}", bookingId, paymentIntent.Status);
                    return BadRequest(new { Error = "Payment not completed." });
                }

                var customer = await _customerService.GetCustomerByIdAsync(booking.UserProfileID);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for UserProfileID {UserProfileID}", booking.UserProfileID);
                    return BadRequest(new { Error = "Customer not found." });
                }

                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = amountInCents / 100m,
                    Currency = "USD",
                    PaymentMethod = "Stripe",
                    TransactionId = paymentIntentId,
                    Status = PaymentStatus.Succeeded,
                    CustomerId = booking.UserProfileID,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Payments.AddAsync(payment);
                booking.Status = BookingStatus.Confirmed;
                _context.Bookings.Update(booking);

                // Send payment confirmation email
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null)
                {
                    var emailBody = _sanitizer.Sanitize(
                        $"<h3>Payment Confirmation</h3><p>Your payment for Booking #{bookingId} ({room.Name}) has been received.</p>");
                    await _emailSender.SendEmailAsync(customer.Email, "Payment Confirmation", emailBody);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Stripe payment completed for Booking {BookingId}, Payment {PaymentId}", bookingId, payment.Id);
                return Ok(new { Message = "Payment successful", PaymentId = payment.Id, ClientSecret = paymentIntent.ClientSecret });
            }
            catch (StripeException ex)
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

        [HttpPost("create-checkout-session")]
        [Authorize]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid checkout session request: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(new { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var userProfileIdClaim = User.FindFirst("UserProfileID")?.Value;
                if (!int.TryParse(userProfileIdClaim, out var userProfileId))
                {
                    _logger.LogWarning("Invalid UserProfileID claim for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    return Unauthorized(new { Error = "Invalid user profile." });
                }

                var booking = await _repository.GetBookingByIdAsync(request.BookingId);
                if (booking == null || booking.UserProfileID != userProfileId)
                {
                    _logger.LogWarning("Booking {BookingId} not found or unauthorized for UserProfileID {UserProfileID}", request.BookingId, userProfileId);
                    return NotFound(new { Error = "Booking not found or unauthorized." });
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    _logger.LogWarning("Booking {BookingId} is not in a payable state: CurrentStatus={Status}", request.BookingId, booking.Status);
                    return BadRequest(new { Error = "Booking is not in a payable state." });
                }

                var session = await _paymentService.CreateCheckoutSessionAsync(
                    request.BookingId,
                    userProfileId,
                    request.SuccessUrl,
                    request.CancelUrl);

                return Ok(new { SessionId = session.Id, SessionUrl = session.Url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session for booking {BookingId}", request.BookingId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("stripe/webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session == null)
                    {
                        _logger.LogWarning("Invalid Stripe session received.");
                        return BadRequest(new { Error = "Invalid session" });
                    }

                    var paymentIntentId = session.PaymentIntentId;
                    if (!session.Metadata.TryGetValue("BookingId", out var bookingIdStr) || !int.TryParse(bookingIdStr, out var bookingId))
                    {
                        _logger.LogWarning("Invalid or missing BookingId in session metadata.");
                        return BadRequest(new { Error = "Invalid BookingId" });
                    }

                    if (!session.Metadata.TryGetValue("CustomerId", out var customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                    {
                        _logger.LogWarning("Invalid or missing CustomerId in session metadata.");
                        return BadRequest(new { Error = "Invalid CustomerId" });
                    }

                    var customer = await _customerService.GetCustomerByIdAsync(customerId);
                    if (customer == null)
                    {
                        _logger.LogWarning("Customer not found for CustomerId {CustomerId}", customerId);
                        return BadRequest(new { Error = "Customer not found." });
                    }

                    var payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = session.AmountTotal!.Value / 100m, // Convert cents to dollars
                        Currency = session.Currency,
                        PaymentMethod = "Stripe",
                        TransactionId = paymentIntentId,
                        Status = PaymentStatus.Succeeded,
                        CustomerId = customerId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Payments.AddAsync(payment);
                    var booking = await _context.Bookings.FindAsync(bookingId);
                    if (booking != null)
                    {
                        booking.Status = BookingStatus.Confirmed;
                        _context.Bookings.Update(booking);

                        // Send payment confirmation email
                        var room = await _context.Rooms.FindAsync(booking.RoomId);
                        if (room != null)
                        {
                            var emailBody = _sanitizer.Sanitize(
                                $"<h3>Payment Confirmation</h3><p>Your payment for Booking #{bookingId} ({room.Name}) has been received.</p>");
                            await _emailSender.SendEmailAsync(customer.Email, "Payment Confirmation", emailBody);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Booking {BookingId} not found.", bookingId);
                        return BadRequest(new { Error = "Booking not found" });
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Payment processed for booking {BookingId}, payment {PaymentId}", bookingId, payment.Id);
                    return Ok();
                }

                _logger.LogWarning("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error processing webhook");
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return StatusCode(500, new { Error = "Internal server error" });
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
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                return Ok(new { SessionId = session.Id, Url = session.Url });
            }
            catch (StripeException ex)
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

                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                    return NotFound(new { Error = "Payment not found." });
                }

                if (payment.Status != PaymentStatus.Succeeded)
                {
                    _logger.LogWarning("Payment {PaymentId} is not eligible for refund", paymentId);
                    return BadRequest(new { Error = "Payment is not eligible for refund." });
                }

                bool success;
                if (payment.PaymentMethod == "PayFast")
                {
                    success = await _paymentService.InitiatePayFastRefundAsync(paymentId, payment.Amount, payment.BookingId);
                }
                else if (payment.PaymentMethod == "Stripe")
                {
                    success = await _paymentService.InitiateStripeRefundAsync(payment.TransactionId);
                }
                else
                {
                    _logger.LogWarning("Unsupported payment method for refund: {PaymentMethod}", payment.PaymentMethod);
                    return BadRequest(new { Error = "Unsupported payment method." });
                }

                if (success)
                {
                    payment.Status = PaymentStatus.Refunded;
                    var booking = await _context.Bookings.FindAsync(payment.BookingId);
                    if (booking != null)
                    {
                        booking.Status = BookingStatus.Refunded;
                        _context.Bookings.Update(booking);
                        await _roomService.UpdateRoomAvailabilityAsync(booking.RoomId, true);
                    }

                    var customer = await _customerService.GetCustomerByIdAsync(payment.CustomerId);
                    if (customer != null)
                    {
                        var emailBody = _sanitizer.Sanitize(
                            $"<h3>Refund Confirmation</h3><p>Your refund for Booking #{payment.BookingId} has been processed.</p>");
                        await _emailSender.SendEmailAsync(customer.Email, "Refund Confirmation", emailBody);
                    }

                    _context.Payments.Update(payment);
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
                return StatusCode(500, new { Error = "Failed to process refund." });
            }
        }
    }

    public class StripePaymentDTO
    {
        [Required]
        public string PaymentToken { get; set; }
    }

    public class CreateCheckoutSessionRequest
    {
        [Required]
        public int BookingId { get; set; }
        [Required]
        public string SuccessUrl { get; set; }
        [Required]
        public string CancelUrl { get; set; }
    }
}