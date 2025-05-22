namespace hotel_and_resort.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string ?FirstName { get; set; }
        public string ?LastName { get; set; }
        public string ?Email { get; set; }
        public string Phone { get; set; }
        public string ?Title { get; set; }
        public string UserId { get; set; }

        // Navigation property
        public ICollection<Booking> Bookings { get; set; }


    }
}
