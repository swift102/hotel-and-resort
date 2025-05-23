using hotel_and_resort.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace hotel_and_resort.Services
{
    public class PaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;

        public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
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
                return paymentIntent.Id; // Return PaymentIntent ID
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating Stripe PaymentIntent");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating Stripe PaymentIntent");
                throw;
            }
        }

        public async Task<Session> CreateCheckoutSessionAsync(int amount, int bookingId, string currency = "usd")
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
                                    Name = $"Booking #{bookingId}",
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = _configuration["Stripe:SuccessUrl"] ?? "http://localhost:4200/success",
                    CancelUrl = _configuration["Stripe:CancelUrl"] ?? "http://localhost:4200/cancel",
                    Metadata = new Dictionary<string, string>
                    {
                        { "BookingId", bookingId.ToString() }
                    }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating Stripe Checkout Session");
                throw;
            }
        }

        public async Task<bool> InitiatePayFastPaymentAsync(int bookingId, decimal amount, string customerEmail)
        {
            try
            {
                // Configuration is handled in PaymentController; this method is a placeholder for future API calls
                _logger.LogInformation("PayFast payment initiated for Booking {BookingId}", bookingId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error initiating Stripe refund for PaymentIntent {PaymentIntentId}", paymentIntentId);
                return false;
            }
        }

        public string GenerateSignature(Dictionary<string, string> data, string passphrase)
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

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public string GenerateSignature(NameValueCollection data, string passphrase)
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

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}