using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.ViewModels
{
    public class AmenitiesDTOs
    {
        // DTO for listing amenities (GET: api/amenities)
        public class AmenityListDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        // DTO for getting detailed amenity info (GET: api/amenities/{id})
        public class AmenityDetailsDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

            // Related rooms
            public List<RoomDTO> Rooms { get; set; }
        }

        // DTO for adding/updating an amenity (POST/PUT: api/amenities)
        public class AmenityCreateUpdateDTO
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        // Simplified Room DTO (used within AmenityDetailsDTO)
        public class RoomDTO
        {
            public int Id { get; set; }
            public string RoomNumber { get; set; }
            public string RoomType { get; set; }
            public string? Name { get; internal set; }
        }
        public class AmenityCreateDTO
        {
            [Required]
            [MaxLength(100)]
            public string Name { get; set; }

            [MaxLength(500)]
            public string Description { get; set; }
        }

    }
}
