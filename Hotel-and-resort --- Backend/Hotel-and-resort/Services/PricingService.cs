using hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace hotel_and_resort.Services
{
    public class PricingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PricingService> _logger;

        public PricingService(AppDbContext context, ILogger<PricingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> CalculateDynamicPrice(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null)
                {
                    _logger.LogWarning("Room not found: {RoomId}", roomId);
                    throw new ArgumentException("Room not found.");
                }

                var days = (checkOut - checkIn).Days;
                if (days <= 0)
                {
                    _logger.LogWarning("Invalid booking duration: {Days} days", days);
                    throw new ArgumentException("Check-out date must be after check-in date.");
                }

                decimal basePrice = room.PricePerNight * days;
                decimal dynamicPrice = basePrice;

                // Example: 20% increase during peak season (December)
                if (checkIn.Month == 12)
                    dynamicPrice *= 1.2m;

                // Example: 10% discount for bookings > 7 days
                if (days > 7)
                    dynamicPrice *= 0.9m;

                room.DynamicPrice = dynamicPrice / days; // Store per-night dynamic price
                await _context.SaveChangesAsync();

                _logger.LogInformation("Calculated dynamic price for Room {RoomId}: {Price} for {Days} days", roomId, dynamicPrice, days);
                return dynamicPrice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating dynamic price for room {RoomId}", roomId);
                throw;
            }
        }
    }
}