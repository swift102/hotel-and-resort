namespace hotel_and_resort.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int CustomerId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int TotalPrice { get; set; }
        public string ?Status { get; set; }


        // Navigation properties
        public Customer Customer { get; set; }
        public ICollection<Room> Rooms { get; set; }
        public ICollection<Payment> Payments { get; set; }

    }
}
