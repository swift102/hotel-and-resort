﻿using hotel_and_resort.Models;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.Models
{

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed,
        Refunded
    }

    public class Booking
    {
      

        [Key]
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int UserProfileID { get; set; }

        public int CustomerId { get; set; }

        [Required]
        public DateTime CheckIn { get; set; }

        [Required]
        public DateTime CheckOut { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public bool IsRefundable { get; set; } // New field

        public string? PaymentIntentId { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
        public Room Room { get; set; }
        public ICollection<Payment> Payments { get; set; }

    }
}


