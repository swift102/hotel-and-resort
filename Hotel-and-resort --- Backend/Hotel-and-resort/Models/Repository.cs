﻿using Microsoft.EntityFrameworkCore;
using hotel_and_resort.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hotel_and_resort.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace hotel_and_resort.Models
{
    public class Repository : IRepository
    {
        private readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }

        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync()) > 0;
        }

        public void Update<T>(T entity) where T : class
        {
            _context.Entry(entity).State = EntityState.Modified;
        }
        // Customer Methods
        public async Task<List<Customer>> GetCustomers()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task<Customer> GetCustomerById(int id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task<Customer> AddCustomer(Customer customer)
        {
            var result = await _context.Customers.AddAsync(customer);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Customer> UpdateCustomer(Customer customer)
        {
            var result = _context.Customers.Update(customer);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Customer> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await SaveChangesAsync();
            }
            return customer;
        }

        // Room Methods
        public async Task<List<Room>> GetRooms()
        {
            return await _context.Rooms.ToListAsync();
        }

        public async Task<Room> GetRoomById(int id)
        {
            return await _context.Rooms.FindAsync(id);
        }

        public async Task<Room> AddRoom(Room room)
        {
            var result = await _context.Rooms.AddAsync(room);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Room> UpdateRoom(Room room)
        {
            var result = _context.Rooms.Update(room);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Room> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await SaveChangesAsync();
            }
            return room;
        }

        public IEnumerable<Booking> GetReservationsForRoom(int roomId)
        {
            // Retrieve reservations for the given room ID from the data store
            return _context.Bookings.Where(r => r.RoomId == roomId);
        }

        // Booking Methods
        public async Task<List<Booking>> GetBookings()
        {
            return await _context.Bookings.ToListAsync();
        }

        public async Task<Booking> GetBookingById(int id)
        {
            return await _context.Bookings.FindAsync(id);
        }

        public async Task<Booking> AddBooking(Booking booking)
        {
            var result = await _context.Bookings.AddAsync(booking);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Booking> UpdateBooking(Booking booking)
        {
            var result = _context.Bookings.Update(booking);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Booking> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await SaveChangesAsync();
            }
            return booking;
        }


        // Payment Methods
        public async Task<List<Payment>> GetPayments()
        {
            return await _context.Payments.Include(p => p.Booking).ToListAsync();
        }

        public async Task<Payment> GetPayment(int id)
        {
            return await _context.Payments.Include(p => p.Booking).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment> AddPayment(Payment payment)
        {
            var result = await _context.Payments.AddAsync(payment);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Payment> DeletePayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await SaveChangesAsync();
            }
            return payment;
        }

        // Image Methods

        // Get all images
        public async Task<List<Image>> GetImages()
        {
            return await _context.Images
                .AsNoTracking()
                .ToListAsync();
        }

        // Get an image by ID
        public async Task<Image> GetImageById(int id)
        {
            return await _context.Images.FindAsync(id);
        }

        // Add a new image
        public async Task<Image> AddImage(Image image)
        {
            _context.Images.Add(image);
            await SaveChangesAsync();
            return image;
        }

        // Update an existing image
        public async Task<Image> UpdateImage(Image image)
        {
            var existingImage = await _context.Images.FindAsync(image.Id);
            if (existingImage == null)
            {
                return null; // Or handle the error as per your requirements
            }

            existingImage.Name = image.Name;
            existingImage.ImagePath = image.ImagePath;
            existingImage.RoomID = image.RoomID;

            _context.Images.Update(existingImage);
            await SaveChangesAsync();
            return existingImage;
        }

        // Delete an image by ID
        public async Task<Image> DeleteImage(int id)
        {
            var image = await _context.Images.FindAsync(id);
            if (image == null)
            {
                return null; // Or handle the error as per your requirements
            }

            _context.Images.Remove(image);
            await SaveChangesAsync();
            return image;
        }

        // Amenity methods
        public async Task<List<Amenities>> GetAmenities()
        {
            return await _context.Amenities.ToListAsync();
        }

        public async Task<Amenities> GetAmenityById(int id)
        {
            return await _context.Amenities.FindAsync(id);
        }

        public async Task<Amenities> AddAmenity(Amenities amenity)
        {
            var result = await _context.Amenities.AddAsync(amenity);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Amenities> UpdateAmenity(Amenities amenity)
        {
            var result = _context.Amenities.Update(amenity);
            await SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Amenities> DeleteAmenity(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity != null)
            {
                _context.Amenities.Remove(amenity);
                await SaveChangesAsync();
            }
            return amenity;
        }


        // Get a user profile by ID
        public async Task<UserProfile> GetUserProfileByID(int userProfileId)
        {
            return await _context.UserProfiles.FindAsync(userProfileId);
        }

        // Get a user by username
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        }

        // UserSession methods

        //public async Task<UserSession> GetUserSessionByIdAsync(string sessionId)
        //{
        //    return await _context.UserSessions
        //        .Include(us => us.User)
        //        .FirstOrDefaultAsync(us => us.SessionID == sessionId);
        //}


        // Save Changes
       
    }
}
