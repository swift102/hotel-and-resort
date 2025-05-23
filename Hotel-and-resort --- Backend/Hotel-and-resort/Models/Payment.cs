using System;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int Amount { get; set; } // In cents

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public string PaymentMethod { get; set; } // "PayFast" or "Stripe"

        [Required]
        public PaymentStatus Status { get; set; }

        public string? StripePaymentIntentId { get; set; }

        public Booking Booking { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
}