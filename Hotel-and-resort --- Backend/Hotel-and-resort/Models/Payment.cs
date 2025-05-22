using hotel_and_resort.Models;

namespace hotel_and_resort.Models
{

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentStatus Status { get; set; }
        public string PaymentMethod { get; set; }
        public string StripePaymentIntentId { get; set; }

        // Navigation property
        public Booking Booking { get; set; }
    }
}

