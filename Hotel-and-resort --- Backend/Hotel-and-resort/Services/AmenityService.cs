using Microsoft.EntityFrameworkCore; 

using hotel_and_resort.Models;

namespace hotel_and_resort.Services
{
    public class AmenityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AmenityService> _logger;

        public AmenityService(AppDbContext context, ILogger<AmenityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Amenities>> GetAmenitiesAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Amenities
                .Include(a => a.Rooms)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Amenities?> GetAmenityByIdAsync(int id)
        {
            return await _context.Amenities
                .Include(a => a.Rooms)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Amenities> AddAmenityAsync(Amenities amenity)
        {
            var result = await _context.Amenities.AddAsync(amenity);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task UpdateAmenityAsync(Amenities amenity)
        {
            _context.Amenities.Update(amenity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAmenityAsync(int id)
        {
            var amenity = await GetAmenityByIdAsync(id);
            if (amenity != null)
            {
                _context.Amenities.Remove(amenity);
                await _context.SaveChangesAsync();
            }
        }
    }
}