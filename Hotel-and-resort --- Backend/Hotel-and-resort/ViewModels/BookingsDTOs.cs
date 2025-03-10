﻿namespace hotel_and_resort.DTOs
{
    // DTO for retrieving booking details (GET endpoints)
    public class BookingResponseDTO
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int CustomerId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int TotalPrice { get; set; }
        public string? Status { get; set; }
    }

    // DTO for creating a new booking (POST endpoint)
    public class BookingCreateDTO
    {
        public int RoomId { get; set; }
        public int CustomerId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int TotalPrice { get; set; }
    }

    // DTO for updating an existing booking (PUT endpoint)
    public class BookingUpdateDTO
    {
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int TotalPrice { get; set; }
        public string? Status { get; set; }
    }
}
