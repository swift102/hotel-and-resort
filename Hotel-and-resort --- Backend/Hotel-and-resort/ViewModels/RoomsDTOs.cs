using hotel_and_resort.ViewModels;
using System.ComponentModel.DataAnnotations;
using static hotel_and_resort.ViewModels.AmenitiesDTOs;

namespace hotel_and_resort.DTOs
{
    public class RoomCreateDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Range(1, int.MaxValue)]
        public int Price { get; set; }
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
        public string? Features { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class RoomReadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string Features { get; set; }
        public bool IsAvailable { get; set; }
        public string Category { get; set; }
        public List<AmenityListDTO> Amenities { get; set; }
        public List<ImageReadDTO> Images { get; set; }
    }

    public class RoomUpdateDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string Features { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class RoomBookingDTO
    {
        public int RoomId { get; set; }
        public int Quantity { get; set; }
    }
}
