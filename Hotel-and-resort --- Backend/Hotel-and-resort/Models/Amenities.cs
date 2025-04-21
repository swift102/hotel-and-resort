using Microsoft.EntityFrameworkCore;

namespace hotel_and_resort.Models
{
    public class Amenities
    {
        public int Id { get; set; }
        public string ?Name { get; set; }
        //public string Icon { get; set; }
        public string? Description { get; set; }
       


        // Navigation property
        public ICollection<Room> Rooms { get; set; }

    }
}
