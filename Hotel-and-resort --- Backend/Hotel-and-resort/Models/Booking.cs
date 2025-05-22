using hotel_and_resort.Models;

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
        internal DateTime CreatedAt;

        public int Id { get; set; }
        public int RoomId { get; set; }
        public int CustomerId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus Status { get; set; } // Add this property
        public bool IsRefundable { get; set; } // New field


        // Navigation properties
        public Customer Customer { get; set; }
        public Room Room { get; set; }
        public ICollection<Payment> Payments { get; set; }

    }
}


