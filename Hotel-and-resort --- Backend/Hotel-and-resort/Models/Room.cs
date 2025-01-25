namespace hotel_and_resort.Models
{
    public class Room
    {
        public int ID { get; set; }
        public string ?Name { get; set; }
        public string ?Description { get; set; }
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string ?Features { get; set; }

        public bool IsAvailable { get; set; }


        // Navigation property for the many-to-many relationship
        public ICollection<Amenities> ?Amenities { get; set; }
        public ICollection<Booking> ?Bookings { get; set; }
        public ICollection<Image> ?Images { get; set; }
    }
}
