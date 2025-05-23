using hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hotel_and_resort.Services
{
    public class RoomService
    {
        private readonly AppDbContext _context;
        private readonly IRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RoomService> _logger;
        private readonly PricingService _pricingService;

        public RoomService(
            AppDbContext context,
            IRepository repository,
            IMemoryCache cache,
            ILogger<RoomService> logger,
            PricingService pricingService)
        {
            _context = context;
            _repository = repository;
            _cache = cache;
            _logger = logger;
            _pricingService = pricingService;
        }

        public async Task<IEnumerable<Room>> GetRoomsAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var cacheKey = $"rooms_page_{page}_size_{pageSize}";
                if (!_cache.TryGetValue(cacheKey, out IEnumerable<Room> rooms))
                {
                    rooms = await _context.Rooms
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    _cache.Set(cacheKey, rooms, TimeSpan.FromMinutes(10));
                    _logger.LogInformation("Cached rooms for key {CacheKey}", cacheKey);
                }
                return rooms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching rooms for page {Page}", page);
                throw;
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
                var room = await _repository.GetRoomByIdAsync(roomId);
                if (room == null)
                {
                    _logger.LogWarning("Room not found: {RoomId}", roomId);
                    throw new InvalidOperationException("Room not found.");
                }

                var days = (checkOut - checkIn).Days;
                if (days <= 0)
                {
                    _logger.LogWarning("Invalid booking duration: {Days} days", days);
                    throw new ArgumentException("Check-out date must be after check-in date.");
                }

                var totalPrice = await _pricingService.CalculateDynamicPrice(roomId, checkIn, checkOut);
                _logger.LogInformation("Calculated price for room {RoomId}: {TotalPrice} for {Days} days", roomId, totalPrice, days);
                return totalPrice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price for room {RoomId}", roomId);
                throw;
            }
        }
    }
}