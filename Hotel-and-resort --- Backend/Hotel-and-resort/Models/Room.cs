using hotel_and_resort.Models;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.Models
{
    public class Room
    {
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string? Name { get; set; }
        public string? Description { get; set; }
        [Range(1, int.MaxValue)]
        public int PricePerNight { get; set; }

        [Required]
        public string RoomType { get; set; }

        [Range(1, 10)]
        public int Capacity { get; set; } = 2; // Default to 2 guests
        public string? Features { get; set; }
        public string Category { get; set; } = "Standard";
        public decimal DynamicPrice { get; set; }

        [Required]
        public decimal BasePrice { get; set; }

        [Required]
        public int RoomNumber { get; set; }
        public bool IsAvailable { get; set; } = true;

        public ICollection<Amenities>? Amenities { get; set; }
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Image>? Images { get; set; }
    }
}

