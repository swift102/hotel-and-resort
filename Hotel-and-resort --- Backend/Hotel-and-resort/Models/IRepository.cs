using Hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hotel_and_resort.Models
{
    public interface IRepository
    {

        public interface IRepository<T> where T : class
        {
            Task<IEnumerable<T>> GetAllAsync();
            Task<T> GetByIdAsync(int id);
            Task AddAsync(T entity);
            Task UpdateAsync(T entity);
            Task DeleteAsync(int id);
        }


        public class Repository<T> : IRepository<T> where T : class
        {
            private readonly AppDbContext _context;

            public Repository(AppDbContext context)
            {
                _context = context;
            }

            public async Task<IEnumerable<T>> GetAllAsync()
            {
                return await _context.Set<T>().ToListAsync();
            }

            public async Task<T> GetByIdAsync(int id)
            {
                return await _context.Set<T>().FindAsync(id);
            }

            public async Task AddAsync(T entity)
            {
                await _context.Set<T>().AddAsync(entity);
                await _context.SaveChangesAsync();
            }

            public async Task UpdateAsync(T entity)
            {
                _context.Set<T>().Update(entity);
                await _context.SaveChangesAsync();
            }

            public async Task DeleteAsync(int id)
            {
                var entity = await _context.Set<T>().FindAsync(id);
                if (entity != null)
                {
                    _context.Set<T>().Remove(entity);
                    await _context.SaveChangesAsync();
                }
            }
        }




        // Room Methods
        Task<List<Room>> GetRoomsAsync();
        Task<Room> GetRoomByIdAsync(int id);
        Task<Room> AddRoomAsync(Room room);
        Task<Room> UpdateRoomAsync(Room room);
        Task<Room> DeleteRoomAsync(int id);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut);
        Task<List<Booking>> GetBookingsByRoomIdAsync(int roomId);
        Task UpdateRoomAvailabilityAsync(int roomId);

        Task<bool> IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut);

        // Booking Methods
        Task<IEnumerable<Booking>> GetAllBookingsAsync(int page, int pageSize);
        Task<Booking> GetBookingByIdAsync(int id);
        Task<Booking> AddBookingAsync(Booking booking);
        Task UpdateBookingAsync(Booking booking);
        Task<bool> RoomExistsAsync(int roomId);
        Task<IDbContextTransaction> BeginTransactionAsync();


        // Payment Methods
     
        Task<Payment> ProcessPaymentAndUpdateBooking(int bookingId, int amount, string paymentToken);

        Task AddPaymentAsync(Payment payment);
        Task<Payment> GetPaymentByIdAsync(int id);
        Task UpdatePaymentAsync(Payment payment);



        // Amenities Methods
        Task<List<Amenities>> GetAmenities();
        Task<Amenities> GetAmenityById(int id);
        Task<Amenities> AddAmenity(Amenities amenity);
        Task<Amenities> UpdateAmenity(Amenities amenity);
        Task<Amenities> DeleteAmenity(int id);


        // Methods for Image entity
            Task<List<Image>> GetImages();
            Task<Image> GetImageById(int id); // Fix: Change method name to match the interface
            Task<Image> AddImage(Image image);
            Task<Image> UpdateImage(Image image);
            Task<Image> DeleteImage(int id);
         
        Task<UserProfile> GetUserProfileByID(int userProfileId);

        Task<User> GetUserByUsernameAsync(string username);


        Task<Payment> ProcessPayment(int bookingId, int amount, string paymentToken);
      


    }
}
