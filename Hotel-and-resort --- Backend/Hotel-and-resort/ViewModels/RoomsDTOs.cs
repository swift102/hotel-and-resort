using hotel_and_resort.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.DTOs
{
    // DTO for getting room details (read-only)
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
        public string? Description { get; set; }
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string? Features { get; set; }
        public bool IsAvailable { get; set; }
        public List<AmenitiesDTOs.AmenityListDTO> Amenities { get; set; } // Added this property
        public List<ImageReadDTO> Images { get; set; }
    }
    // DTO for updating room details (write-only)
    public class RoomUpdateDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string Features { get; set; }
        public bool IsAvailable { get; set; }
    }
}
