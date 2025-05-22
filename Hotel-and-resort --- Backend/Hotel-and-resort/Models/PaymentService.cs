using hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Net.Http;
using System.Text;
using System.Web;

namespace Hotel_and_resort.Models
{
    public class PaymentService
    {
        private readonly string _stripeSecretKey; private readonly IConfiguration _configuration; private readonly ILogger _logger; private readonly HttpClient _httpClient;
        public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            _logger = logger;
            _httpClient = httpClient;
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public async Task<string> CreatePaymentIntentAsync(int amount, string currency = "usd")
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = amount, // Amount in cents
                    Currency = currency,
                    PaymentMethodTypes = new List<string> { "card" }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);
                _logger.LogInformation("Stripe PaymentIntent created: {PaymentIntentId}", paymentIntent.Id);
                return paymentIntent.ClientSecret;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating Stripe PaymentIntent");
                throw;
            }
        }

        public async Task<Session> CreateCheckoutSessionAsync(int amount, string currency = "usd")
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amount, // Amount in cents
                            Currency = currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Hotel Booking",
                            },
                        },
                        Quantity = 1,
                    },
                },
                    Mode = "payment",
                    SuccessUrl = _configuration["Stripe:SuccessUrl"] ?? "http://localhost:4200/success",
                    CancelUrl = _configuration["Stripe:CancelUrl"] ?? "http://localhost:4200/cancel",
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);
                _logger.LogInformation("Stripe Checkout Session created: {SessionId}", session.Id);
                return session;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating Stripe Checkout Session");
                throw;
            }
        }

        public async Task<bool> InitiatePayFastPaymentAsync(int bookingId, decimal amount, string customerEmail)
        {
            try
            {
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
                { "amount", amount.ToString("F2") },
                { "item_name", $"Booking #{bookingId}" },
                { "email_address", customerEmail },
                { "m_payment_id", bookingId.ToString() }
            };

                var signature = GenerateSignature(data, passphrase);
                data.Add("signature", signature);

                var query = string.Join("&", data.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
                var redirectUrl = $"https://sandbox.payfast.co.za/eng/process?{query}";

                // Log the redirect URL for demo purposes
                _logger.LogInformation("PayFast payment initiated for Booking {BookingId}: {RedirectUrl}", bookingId, redirectUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PayFast payment for Booking {BookingId}", bookingId);
                return false;
            }
        }

        public async Task<bool> InitiatePayFastRefundAsync(int paymentId, decimal amount, int bookingId)
        {
            try
            {
                var merchantId = _configuration["PayFast:MerchantId"] ?? "10000100";
                var merchantKey = _configuration["PayFast:MerchantKey"] ?? "46f0cd694581a";
                var passphrase = _configuration["PayFast:Passphrase"] ?? "test_passphrase";

                var refundData = new Dictionary<string, string>
            {
                { "merchant_id", merchantId },
                { "merchant_key", merchantKey },
                { "amount", amount.ToString("F2") },
                { "m_payment_id", bookingId.ToString() }
            };

                var signature = GenerateSignature(refundData, passphrase);
                refundData.Add("signature", signature);

                var content = new FormUrlEncodedContent(refundData);
                var response = await _httpClient.PostAsync("https://api.payfast.co.za/refunds", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("PayFast refund initiated for Payment {PaymentId}", paymentId);
                    return true;
                }

                _logger.LogWarning("PayFast refund failed for Payment {PaymentId}", paymentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PayFast refund for Payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<bool> InitiateStripeRefundAsync(string paymentIntentId)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId
                };
                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                if (refund.Status == "succeeded")
                {
                    _logger.LogInformation("Stripe refund succeeded for PaymentIntent {PaymentIntentId}", paymentIntentId);
                    return true;
                }

                _logger.LogWarning("Stripe refund failed for PaymentIntent {PaymentIntentId}", paymentIntentId);
                return false;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error initiating Stripe refund for PaymentIntent {PaymentIntentId}", paymentIntentId);
                return false;
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


    }
}