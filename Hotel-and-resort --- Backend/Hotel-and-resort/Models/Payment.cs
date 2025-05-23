using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace hotel_and_resort.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public string PaymentMethod { get; set; } // "PayFast" or "Stripe"

        [Required]
        public PaymentStatus Status { get; set; }

        [Required]
        public string StatusMessage { get; set; } = string.Empty;

        [Required]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        public string Currency { get; set; } = "ZAR"; // Default to South African Rand

        public int CustomerId { get; set; } // Foreign key to Customer table

        public string? StripePaymentIntentId { get; set; }

 
    public DateTime CreatedAt { get; set; }
        public Booking Booking { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded,
        Succeeded
    }
}