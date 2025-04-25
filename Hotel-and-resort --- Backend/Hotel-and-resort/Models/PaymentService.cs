using hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Hotel_and_resort.Models
{
    public class PaymentService
    {
        private readonly string _stripeSecretKey;

        public PaymentService(IConfiguration configuration)
        {
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public async Task<string> CreatePaymentIntentAsync(int amount, string currency = "usd")
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = amount * 100, // Stripe uses cents, so multiply by 100
                Currency = currency,
                PaymentMethodTypes = new List<string> { "card" }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return paymentIntent.ClientSecret;
        }

        public async Task<Session> CreateCheckoutSessionAsync(int amount, string currency = "usd")
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
                        UnitAmount = amount * 100, // Stripe uses cents
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
                SuccessUrl = "https://yourwebsite.com/success", // Replace with your success URL
                CancelUrl = "https://yourwebsite.com/cancel",   // Replace with your cancel URL
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session;
        }

       
    }
}


//public interface IPaymentGateway
//{
//    Task<string> CreatePaymentIntentAsync(int amount, string currency);
//    Task<Session> CreateCheckoutSessionAsync(int amount, string currency);
//}

//public class StripePaymentGateway : IPaymentGateway
//{
//    public StripePaymentGateway(IConfiguration configuration)
//    {
//        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
//    }

//    public async Task<string> CreatePaymentIntentAsync(int amount, string currency)
//    {
//        var options = new PaymentIntentCreateOptions
//        {
//            Amount = amount * 100,
//            Currency = currency,
//            PaymentMethodTypes = new List<string> { "card" }
//        };

//        var service = new PaymentIntentService();
//        var paymentIntent = await service.CreateAsync(options);

//        return paymentIntent.ClientSecret;
//    }

//    public async Task<Session> CreateCheckoutSessionAsync(int amount, string currency)
//    {
//        var options = new SessionCreateOptions
//        {
//            PaymentMethodTypes = new List<string> { "card" },
//            LineItems = new List<SessionLineItemOptions>
//            {
//                new SessionLineItemOptions
//                {
//                    PriceData = new SessionLineItemPriceDataOptions
//                    {
//                        UnitAmount = amount * 100,
//                        Currency = currency,
//                        ProductData = new SessionLineItemPriceDataProductDataOptions
//                        {
//                            Name = "Hotel Booking",
//                        },
//                    },
//                    Quantity = 1,
//                },
//            },
//            Mode = "payment",
//            SuccessUrl = "https://yourwebsite.com/success",
//            CancelUrl = "https://yourwebsite.com/cancel",
//        };

//        var service = new SessionService();
//        return await service.CreateAsync(options);
//    }
//}


//public async Task<Payment> ProcessPayment(int bookingId, int amount, string paymentToken)
//{
//    // Create a new Payment entity
//    var payment = new Payment
//    {
//        BookingId = bookingId,
//        Amount = amount,
//        PaymentDate = DateTime.UtcNow,
//        Status = PaymentStatus.Pending // Initial status
//    };

//    try
//    {
//        // Create a PaymentIntent using Stripe
//        var options = new PaymentIntentCreateOptions
//        {
//            Amount = amount * 100, // Stripe uses cents, so multiply by 100
//            Currency = "zar", // Replace with your currency
//            PaymentMethod = paymentToken, // Token from the frontend
//            Confirm = true, // Automatically confirm the payment
//            OffSession = true, // Payment is happening without the customer being present
//        };

//        var service = new PaymentIntentService();
//        var paymentIntent = await service.CreateAsync(options);

//        // Check if the payment was successful
//        if (paymentIntent.Status == "succeeded")
//        {
//            payment.Status = PaymentStatus.Completed;
//        }
//        else
//        {
//            payment.Status = PaymentStatus.Failed;
//        }
//    }
//    catch (StripeException ex)
//    {
//        // Handle Stripe-specific errors
//        payment.Status = PaymentStatus.Failed;
//        _logger.LogError(ex, "Stripe payment failed for booking {BookingId}", bookingId);
//    }
//    catch (Exception ex)
//    {
//        // Handle other errors
//        payment.Status = PaymentStatus.Failed;
//        _logger.LogError(ex, "Payment processing failed for booking {BookingId}", bookingId);
//    }

//    // Save the payment to the database
//    await _context.Payments.AddAsync(payment);
//    await _context.SaveChangesAsync();

//    return payment;
//}