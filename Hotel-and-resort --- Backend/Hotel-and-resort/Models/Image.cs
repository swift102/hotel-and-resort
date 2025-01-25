namespace hotel_and_resort.Models
{
    public class Image
    {
        public int Id { get; set; } // Primary key
        public string ?Name { get; set; }
        public string? ImagePath { get; set; }
        public int RoomID { get; set; } // Foreign key

        // Navigation property
        public Room Room { get; set; }
    }

}
