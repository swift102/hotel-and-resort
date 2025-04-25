using hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Hotel_and_resort.Services
{
    namespace hotel_and_resort.Services
    {
        public class RoomService
        {
            private readonly AppDbContext _context;
            private readonly IMemoryCache _cache;
            private readonly ILogger<RoomService> _logger;

            public RoomService(AppDbContext context, IMemoryCache cache, ILogger<RoomService> logger)
            {
                _context = context;
                _cache = cache;
                _logger = logger;
            }

            public async Task<IEnumerable<Room>> GetRoomsAsync(int page = 1, int pageSize = 10)
            {
                var cacheKey = $"rooms_page_{page}_size_{pageSize}";
                if (!_cache.TryGetValue(cacheKey, out IEnumerable<Room> rooms))
                {
                    rooms = await _context.Rooms
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    _cache.Set(cacheKey, rooms, TimeSpan.FromMinutes(10));
                }
                return rooms;
            }

            public async Task<Room?> GetRoomByIdAsync(int id)
            {
                return await _context.Rooms
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }

            public async Task<Room> AddRoomAsync(Room room)
            {
                var result = await _context.Rooms.AddAsync(room);
                await _context.SaveChangesAsync();
                return result.Entity;
            }

            public async Task UpdateRoomAsync(Room room)
            {
                _context.Rooms.Update(room);
                await _context.SaveChangesAsync();
            }

            public async Task DeleteRoomAsync(int id)
            {
                var room = await GetRoomByIdAsync(id);
                if (room != null)
                {
                    _context.Rooms.Remove(room);
                    await _context.SaveChangesAsync();
                }
            }

            public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
            {
                var overlappingBookings = await _context.Bookings
                    .Where(b => b.RoomId == roomId && b.CheckIn < checkOut && b.CheckOut > checkIn)
                    .AnyAsync();
                return !overlappingBookings;
            }
        }
    }
}
