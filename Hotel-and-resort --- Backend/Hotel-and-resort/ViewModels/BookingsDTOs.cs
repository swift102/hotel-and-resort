using hotel_and_resort.Models;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.DTOs
{
    // DTO for retrieving booking details (GET endpoints)
    public class BookingResponseDTO
    {

        public int Id { get; set; }
        public int RoomId { get; set; }
        public int CustomerId { get; set; }
        [Required]
        public DateTime CheckIn { get; set; }
        [Required]
        public DateTime CheckOut { get; set; }
        [Range(0, int.MaxValue)]
        public decimal TotalPrice { get; set; }
        [MaxLength(50)]
        public string Status { get; set; }
        [Range(1, 10)]
        public int GuestCount { get; set; }
        public bool IsRefundable { get; set; }
        public DateTime? CancelledAt { get; set; }
        [Range(0, 100)]
        public decimal RefundPercentage { get; set; }
    }


    // DTO for updating an existing booking (PUT endpoint)
    public class BookingUpdateDTO
    {
        [Required]
        public DateTime CheckIn { get; set; }
        [Required]
        public DateTime CheckOut { get; set; }
        [Range(0, int.MaxValue)]
        public decimal TotalPrice { get; set; }
        [MaxLength(50)]
        public string Status { get; set; }
        [Range(1, 10)]
        public int GuestCount { get; set; }
    }


    public class BookingCreateDTO
    {
        public List<Room> Rooms { get; set; } = new();
        [Required]
        public int RoomId { get; set; }
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string CustomerEmail { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string CustomerFirstName { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string CustomerLastName { get; set; } = string.Empty;
        [Required]
        [Phone]
        [MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;
        [MaxLength(50)]
        public string CustomerTitle { get; set; } = string.Empty;
        [Required]
        public DateTime CheckIn { get; set; }
        [Required]
        public DateTime CheckOut { get; set; }
        public bool IsRefundable { get; set; }
        [Range(1, 10)]
        public int GuestCount { get; set; } = 1;
    }

    public class BookingStatusDTO
    {
        [Required]
        public BookingStatus Status { get; set; }
    }

    public class BookingCancellationDTO
    {
        [Required]
        public int BookingId { get; set; }
        public string CancellationReason { get; set; }
    }
}
