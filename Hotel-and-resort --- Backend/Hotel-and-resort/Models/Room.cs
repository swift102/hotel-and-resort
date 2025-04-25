using hotel_and_resort.Models;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.Models
{
    public class Room
    {
        public int Id { get; set; } // Changed from ID to Id
        [Required, MaxLength(100)]
        public string? Name { get; set; }
        public string? Description { get; set; }
        [Range(1, int.MaxValue)]
        public int Price { get; set; }
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
        public string? Features { get; set; }
        public string Category { get; set; } = "Standard"; // Default value
        public decimal DynamicPrice { get; set; } // Consider using this for dynamic pricing logic
        public bool IsAvailable { get; set; } = true;

        public ICollection<Amenities>? Amenities { get; set; }
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Image>? Images { get; set; }
    }
}

