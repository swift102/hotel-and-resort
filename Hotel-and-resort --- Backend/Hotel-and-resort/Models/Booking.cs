using hotel_and_resort.Models;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.Models
{

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed,
        Refunded
    }

    public class Booking
    {
      

        [Key]
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int UserProfileID { get; set; }

        public int CustomerId { get; set; }

        [Required]
        public DateTime CheckIn { get; set; }

        [Required]
        public DateTime CheckOut { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public bool IsRefundable { get; set; } 

        [Range(1, 10)]
        public int GuestCount { get; set; } = 1; // Default to 1 guest

        [Range(0, 100)]
        public decimal RefundPercentage { get; set; } // e.g., 100 for full refund, 50 for partial refund
        public string? PaymentIntentId { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
        public Room Room { get; set; }
        public ICollection<Payment> Payments { get; set; }

    }
}


