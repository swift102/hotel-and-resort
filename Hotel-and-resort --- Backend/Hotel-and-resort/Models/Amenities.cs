using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.Models
{
    public class Amenities
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }


        // Navigation property
        public ICollection<Room> Rooms { get; set; }

    }
}
