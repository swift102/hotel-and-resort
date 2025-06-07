using hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

namespace hotel_and_resort.Services
{
    public class RoomService
    {
        private readonly AppDbContext _context;
        private readonly IRepository _repository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<RoomService> _logger;
        private readonly PricingService _pricingService;

        public RoomService(
            AppDbContext context,
            IRepository repository,
           IDistributedCache cache,
            ILogger<RoomService> logger,
            PricingService pricingService)
        {
            _context = context;
            _repository = repository;
            _cache = cache;
            _logger = logger;
            _pricingService = pricingService;
        }

        public async Task<IEnumerable<Room>> GetRooms()
        {
            try
            {
                var cacheKey = "rooms_all";
                var cachedRooms = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedRooms))
                {
                    _logger.LogInformation("Cache hit for rooms");
                    return JsonSerializer.Deserialize<IEnumerable<Room>>(cachedRooms);
                }

                var rooms = await _repository.GetRoomsAsync();
                var serializedRooms = JsonSerializer.Serialize(rooms);
                await _cache.SetStringAsync(cacheKey, serializedRooms, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                _logger.LogInformation("Cache miss for rooms, fetched from database");
                return rooms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching rooms");
                throw new RoomServiceException("Failed to retrieve rooms.", ex);
            }
        }



        public async Task<Room?> GetRoomByIdAsync(int id)
        {
            try
            {
                return await _context.Rooms
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching room with ID {RoomId}", id);
                throw;
            }
        }

        public async Task<Room> AddRoomAsync(Room room)
        {
            try
            {
                var result = await _context.Rooms.AddAsync(room);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added room with ID {RoomId}", room.Id);
                return result.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding room");
                throw;
            }
        }

        public async Task UpdateRoomAsync(Room room)
        {
            try
            {
                _context.Rooms.Update(room);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated room with ID {RoomId}", room.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room with ID {RoomId}", room.Id);
                throw;
            }
        }


        public async Task UpdateRoomAvailabilityAsync(int roomId, bool isAvailable)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                throw new ArgumentException($"Room with ID {roomId} not found.");
            }

            room.IsAvailable = isAvailable;
            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRoomAsync(int id)
        {
            try
            {
                var room = await GetRoomByIdAsync(id);
                if (room != null)
                {
                    _context.Rooms.Remove(room);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted room with ID {RoomId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room with ID {RoomId}", id);
                throw;
            }
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var exists = await _repository.RoomExistsAsync(roomId);
                if (!exists)
                {
                    _logger.LogWarning("Room not found: {RoomId}", roomId);
                    return false;
                }

                var isAvailable = await _repository.IsRoomAvailableAsync(roomId, checkIn, checkOut);
                if (!isAvailable)
                {
                    _logger.LogInformation("Room {RoomId} is not available for {CheckIn} to {CheckOut}", roomId, checkIn, checkOut);
                }

                return isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for room {RoomId}", roomId);
                throw;
            }
        }

        public async Task<decimal> CalculatePrice(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null)
                {
                    _logger.LogWarning("Room not found: {RoomId}", roomId);
                    throw new InvalidOperationException($"Room with ID {roomId} not found.");
                }

                if (checkOut <= checkIn)
                {
                    _logger.LogWarning("Invalid dates: CheckOut {CheckOut} is not after CheckIn {CheckIn}", checkOut, checkIn);
                    throw new ArgumentException("Check-out date must be after check-in date.");
                }

                // Calculate number of nights
                var nights = (checkOut - checkIn).Days;

                // Base price (assumes Room model has BasePrice)
                var basePrice = room.BasePrice;

                // Apply dynamic pricing (example: +10% during peak season, Dec-Jan)
                var isPeakSeason = checkIn.Month == 12 || checkIn.Month == 1;
                var priceMultiplier = isPeakSeason ? 1.1m : 1.0m;

                var totalPrice = basePrice * nights * priceMultiplier;

                _logger.LogInformation("Calculated price for room {RoomId}: {TotalPrice:C} for {Nights} nights", roomId, totalPrice, nights);
                return Math.Round(totalPrice, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price for room {RoomId}", roomId);
                throw;
            }
        }
    }
}

public class RoomServiceException : Exception
{
    public RoomServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}