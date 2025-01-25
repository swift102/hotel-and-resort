using Microsoft.EntityFrameworkCore;
using hotel_and_resort.Models;
using System.Collections.Generic;
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
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Customer> UpdateCustomer(Customer customer)
        {
            var result = _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Customer> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Room> UpdateRoom(Room room)
        {
            var result = _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Room> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
            return room;
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
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Booking> UpdateBooking(Booking booking)
        {
            var result = _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Booking> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Payment> DeletePayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Amenities> UpdateAmenity(Amenities amenity)
        {
            var result = _context.Amenities.Update(amenity);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Amenities> DeleteAmenity(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity != null)
            {
                _context.Amenities.Remove(amenity);
                await _context.SaveChangesAsync();
            }
            return amenity;
        }


        // Save Changes
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
