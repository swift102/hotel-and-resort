using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Microsoft.EntityFrameworkCore;

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
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) throw new ArgumentException("Room not found.");

            decimal basePrice = room.Price;
            decimal dynamicPrice = basePrice;

            // Example: 20% increase during peak season (December)
            if (checkIn.Month == 12)
                dynamicPrice *= 1.2m;

            // Example: 10% discount for bookings > 7 days
            if ((checkOut - checkIn).Days > 7)
                dynamicPrice *= 0.9m;

            _logger.LogInformation("Calculated dynamic price for Room {RoomId}: {Price}", roomId, dynamicPrice);
            return dynamicPrice;
        }
    }
}

