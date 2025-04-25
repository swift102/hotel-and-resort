using Hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hotel_and_resort.Models
{
    public interface IRepository
    {
        void Add<T>(T entity) where T : class;
        void Delete<T>(T entity) where T : class;
        Task<bool> SaveChangesAsync();
        void Update<T>(T entity) where T : class;


        // Customer Methods
        Task<List<Customer>> GetCustomers();
        Task<Customer> GetCustomerById(int id);
        Task<Customer> AddCustomer(Customer customer);
        Task<Customer> UpdateCustomer(Customer customer);
        Task<Customer> DeleteCustomer(int id);

        // Room Methods
        Task<List<Room>> GetRooms();
        Task<Room> GetRoomById(int id);
        Task<Room> AddRoom(Room room);
        Task<Room> UpdateRoom(Room room);
        Task<Room> DeleteRoom(int id);

        // Booking Methods
        Task<List<Booking>> GetBookings();
        Task<Booking> GetBookingById(int id);
        Task<Booking> AddBooking(Booking booking);
        Task<Booking> UpdateBooking(Booking booking);
        Task<Booking> DeleteBooking(int id);

        // Payment Methods
        Task<List<Payment>> GetPayments();
        Task<Payment> GetPayment(int id);
        Task<Payment> AddPayment(Payment payment);
        Task<Payment> DeletePayment(int id);

        
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

        //Task<UserSession> GetUserSessionByIdAsync(string sessionId);

        // Image Methods
        //Task<List<Image>> GetImages();
        //Task<Image> GetImage(int id);
        //Task<Image> AddImage(Image image);
        //Task<Image> DeleteImage(int id);



    }
}
