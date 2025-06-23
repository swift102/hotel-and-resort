using System.ComponentModel.DataAnnotations.Schema;

namespace hotel_and_resort.Models
{
    public class Image
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ImagePath { get; set; } // Store relative path
        public string? AltText { get; set; } // For accessibility
        public int RoomID { get; set; }

        // Navigation property
        public Room Room { get; set; }

        // Computed property for full URL
        [NotMapped]
        public string FullImageUrl => $"/images/rooms/{ImagePath}";
    }

}
