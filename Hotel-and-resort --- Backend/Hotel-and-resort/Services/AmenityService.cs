using Ganss.Xss;
using hotel_and_resort.Models;
using Hotel_and_resort.Data;
using Microsoft.EntityFrameworkCore; 
using Microsoft.Extensions.Caching.Memory;

namespace hotel_and_resort.Services
{
    public class AmenityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AmenityService> _logger;
        private readonly IHtmlSanitizer _sanitizer;
        private readonly IMemoryCache _cache;
        private const string AmenitiesCacheKey = "AmenitiesList";
        private const int CacheDurationMinutes = 5;

        public AmenityService(
            AppDbContext context,
            ILogger<AmenityService> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _sanitizer = new HtmlSanitizer();
            _cache = cache;
        }

        public async Task<IEnumerable<Amenities>> GetAmenitiesAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                // Check cache
                if (_cache.TryGetValue($"{AmenitiesCacheKey}_p{page}_ps{pageSize}", out IEnumerable<Amenities> cachedAmenities))
                {
                    _logger.LogInformation("Retrieved amenities from cache, page {Page}, pageSize {PageSize}", page, pageSize);
                    return cachedAmenities;
                }

                var amenities = await _context.Amenities
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Cache results
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(CacheDurationMinutes));
                _cache.Set($"{AmenitiesCacheKey}_p{page}_ps{pageSize}", amenities, cacheEntryOptions);

                _logger.LogInformation("Retrieved amenities from database, page {Page}, pageSize {PageSize}", page, pageSize);
                return amenities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenities, page {Page}, pageSize {PageSize}", page, pageSize);
                throw new AmenityServiceException("Failed to retrieve amenities.", ex);
            }
        }

        public async Task<Amenities?> GetAmenityByIdAsync(int id)
        {
            try
            {
                return await _context.Amenities
                    .Include(a => a.Rooms)
                    .FirstOrDefaultAsync(a => a.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenity with ID {AmenityId}", id);
                throw new AmenityServiceException($"Failed to retrieve amenity with ID {id}.", ex);
            }
        }

        public async Task<Amenities> AddAmenityAsync(Amenities amenity)
        {
            if (amenity == null)
            {
                _logger.LogWarning("Attempted to add null amenity.");
                throw new ArgumentNullException(nameof(amenity));
            }

            try
            {
                // Sanitize inputs
                amenity.Name = _sanitizer.Sanitize(amenity.Name);
                amenity.Description = _sanitizer.Sanitize(amenity.Description);

                // Validate inputs
                if (string.IsNullOrWhiteSpace(amenity.Name))
                {
                    _logger.LogWarning("Invalid amenity data: Name is required.");
                    throw new AmenityValidationException("Amenity name is required.");
                }
                if (amenity.Name.Length > 100)
                {
                    _logger.LogWarning("Invalid amenity data: Name exceeds 100 characters.");
                    throw new AmenityValidationException("Amenity name cannot exceed 100 characters.");
                }
                if (!string.IsNullOrEmpty(amenity.Description) && amenity.Description.Length > 500)
                {
                    _logger.LogWarning("Invalid amenity data: Description exceeds 500 characters.");
                    throw new AmenityValidationException("Amenity description cannot exceed 500 characters.");
                }

                // Check for duplicate name
                if (await _context.Amenities.AnyAsync(a => a.Name == amenity.Name))
                {
                    _logger.LogWarning("Duplicate amenity name detected: {Name}", amenity.Name);
                    throw new DuplicateAmenityException($"Amenity with name {amenity.Name} already exists.");
                }

                var result = await _context.Amenities.AddAsync(amenity);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove(AmenitiesCacheKey);
                _logger.LogInformation("Cache invalidated for amenities after adding amenity {AmenityId}", result.Entity.Id);

                // Publish event
                await PublishAmenityEvent(result.Entity, "created");

                _logger.LogInformation("Amenity added successfully: {AmenityId}", result.Entity.Id);
                return result.Entity;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error adding amenity with name {Name}", amenity.Name);
                throw new AmenityServiceException("Failed to add amenity due to database error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding amenity with name {Name}", amenity.Name);
                throw new AmenityServiceException("Failed to add amenity.", ex);
            }
        }

        public async Task UpdateAmenityAsync(Amenities amenity)
        {
            if (amenity == null)
            {
                _logger.LogWarning("Attempted to update null amenity.");
                throw new ArgumentNullException(nameof(amenity));
            }

            try
            {
                // Sanitize inputs
                amenity.Name = _sanitizer.Sanitize(amenity.Name);
                amenity.Description = _sanitizer.Sanitize(amenity.Description);

                // Validate inputs
                if (string.IsNullOrWhiteSpace(amenity.Name))
                {
                    _logger.LogWarning("Invalid amenity data: Name is required.");
                    throw new AmenityValidationException("Amenity name is required.");
                }
                if (amenity.Name.Length > 100)
                {
                    _logger.LogWarning("Invalid amenity data: Name exceeds 100 characters.");
                    throw new AmenityValidationException("Amenity name cannot exceed 100 characters.");
                }
                if (!string.IsNullOrEmpty(amenity.Description) && amenity.Description.Length > 500)
                {
                    _logger.LogWarning("Invalid amenity data: Description exceeds 500 characters.");
                    throw new AmenityValidationException("Amenity description cannot exceed 500 characters.");
                }

                // Check for duplicate name
                if (await _context.Amenities.AnyAsync(a => a.Name == amenity.Name && a.Id != amenity.Id))
                {
                    _logger.LogWarning("Duplicate amenity name detected: {Name}", amenity.Name);
                    throw new DuplicateAmenityException($"Amenity with name {amenity.Name} already exists.");
                }

                var existingAmenity = await _context.Amenities.FindAsync(amenity.Id);
                if (existingAmenity == null)
                {
                    _logger.LogWarning("Amenity not found for update: {AmenityId}", amenity.Id);
                    throw new AmenityNotFoundException($"Amenity with ID {amenity.Id} not found.");
                }

                existingAmenity.Name = amenity.Name;
                existingAmenity.Description = amenity.Description;
                // Note: Rooms are managed via many-to-many relationship, not updated here

                _context.Amenities.Update(existingAmenity);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove(AmenitiesCacheKey);
                _logger.LogInformation("Cache invalidated for amenities after updating amenity {AmenityId}", existingAmenity.Id);

                // Publish event
                await PublishAmenityEvent(existingAmenity, "updated");

                _logger.LogInformation("Amenity updated successfully: {AmenityId}", existingAmenity.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating amenity with ID {AmenityId}", amenity.Id);
                throw new AmenityServiceException($"Failed to update amenity with ID {amenity.Id} due to database error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating amenity with ID {AmenityId}", amenity.Id);
                throw new AmenityServiceException($"Failed to update amenity with ID {amenity.Id}.", ex);
            }
        }

        public async Task DeleteAmenityAsync(int id)
        {
            try
            {
                var amenity = await _context.Amenities.FindAsync(id);
                if (amenity == null)
                {
                    _logger.LogWarning("Amenity not found for deletion: {AmenityId}", id);
                    throw new AmenityNotFoundException($"Amenity with ID {id} not found.");
                }

                _context.Amenities.Remove(amenity);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove(AmenitiesCacheKey);
                _logger.LogInformation("Cache invalidated for amenities after deleting amenity {AmenityId}", id);

                // Publish event
                await PublishAmenityEvent(amenity, "deleted");

                _logger.LogInformation("Amenity deleted successfully: {AmenityId}", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting amenity with ID {AmenityId}", id);
                throw new AmenityServiceException($"Failed to delete amenity with ID {id} due to database error.", ex);
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "Error deleting amenity with ID {AmenityId}", id);
            throw new AmenityServiceException($"Failed to delete amenity with ID {id}.", ex);
            }
        }

        private async Task PublishAmenityEvent(Amenities amenity, string action)
        {
            // Placeholder for event publishing (e.g., via MediatR or event bus)
            _logger.LogInformation("Published AmenityEvent for amenity {AmenityId}, action {Action}", amenity.Id, action);
            // Future: Publish to message queue or event handler
            await Task.CompletedTask;
        }
    }

    public class AmenityServiceException : Exception
    {
        public AmenityServiceException(string message) : base(message) { }
        public AmenityServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AmenityValidationException : Exception
    {
        public AmenityValidationException(string message) : base(message) { }
    }

    public class DuplicateAmenityException : Exception
    {
        public DuplicateAmenityException(string message) : base(message) { }
    }

    public class AmenityNotFoundException : Exception
    {
        public AmenityNotFoundException(string message) : base(message) { }
    }
}