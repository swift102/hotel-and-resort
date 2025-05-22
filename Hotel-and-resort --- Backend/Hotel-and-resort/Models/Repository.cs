using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;


namespace hotel_and_resort.Models
{
    public class Repository : IRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<Repository> _logger;



        public Repository(AppDbContext context, ILogger<Repository> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task<Room> GetRoomByIdAsync(int id)
        {
            try
            {
                return await _context.Rooms.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching room with ID {RoomId}", id);
                throw new RepositoryException($"Failed to retrieve room with ID {id}.", ex);
            }
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

        public async Task<IEnumerable<Booking>> GetAllBookingsAsync(int page, int pageSize)
        {
            try
            {
                return await _context.Bookings
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings, page {Page}, pageSize {PageSize}", page, pageSize);
                throw new RepositoryException("Failed to retrieve bookings.", ex);
            }
        }

        public async Task<Booking> GetBookingByIdAsync(int id)
        {
            try
            {
                return await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking with ID {BookingId}", id);
                throw new RepositoryException($"Failed to retrieve booking with ID {id}.", ex);
            }
        }

        public async Task<Booking> AddBookingAsync(Booking booking)
        {
            if (booking == null)
            {
                _logger.LogWarning("Attempted to add null booking.");
                throw new ArgumentNullException(nameof(booking));
            }

            try
            {
                var result = await _context.Bookings.AddAsync(booking);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Booking added: {BookingId}", result.Entity.Id);
                return result.Entity;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error adding booking for room {RoomId}", booking.RoomId);
                throw new RepositoryException("Failed to add booking due to database error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding booking for room {RoomId}", booking.RoomId);
                throw new RepositoryException("Failed to add booking.", ex);
            }
        }

        public async Task UpdateBookingAsync(Booking booking)
        {
            if (booking == null)
            {
                _logger.LogWarning("Attempted to update null booking.");
                throw new ArgumentNullException(nameof(booking));
            }

            try
            {
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Booking updated: {BookingId}", booking.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating booking {BookingId}", booking.Id);
                throw new RepositoryException($"Failed to update booking with ID {booking.Id} due to database error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking {BookingId}", booking.Id);
                throw new RepositoryException($"Failed to update booking with ID {booking.Id}.", ex);
            }
        }

        public async Task<bool> IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                // Use FOR UPDATE to lock rows (requires transaction)
                var isBooked = await _context.Bookings
                    .Where(b => b.RoomId == roomId &&
                                b.Status != BookingStatus.Cancelled &&
                                checkIn < b.CheckOut && checkOut > b.CheckIn)
                    .Select(b => b.Id) // Minimize data
                    .Take(1) // Optimize for existence check
                    .ToListAsync();

                return !isBooked.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for room {RoomId}", roomId);
                throw new RepositoryException($"Failed to check availability for room {roomId}.", ex);
            }
        }

        public async Task<bool> RoomExistsAsync(int roomId)
        {
            try
            {
                return await _context.Rooms.AnyAsync(r => r.Id == roomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of room {RoomId}", roomId);
                throw new RepositoryException($"Failed to check existence of room {roomId}.", ex);
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            try
            {
                return await _context.Database.BeginTransactionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting database transaction.");
                throw new RepositoryException("Failed to start transaction.", ex);
            }
        }
    

    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base(message) { }
        public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
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

        //public async Task UpdateRoomAvailability(int roomId)
        //{
        //    var room = await _context.Rooms.FindAsync(roomId);
        //    if (room != null)
        //    {
        //        room.IsAvailable = false; // Example logic to mark the room as unavailable
        //        _context.Rooms.Update(room);
        //        await _context.SaveChangesAsync();
        //    }
        //}


        public async Task UpdateRoomAvailability(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            var hasActiveBookings = await _context.Bookings
                .AnyAsync(b => b.RoomId == roomId && b.Status == BookingStatus.Confirmed);

            room.IsAvailable = !hasActiveBookings;
            await _context.SaveChangesAsync();
        }

        public async Task<List<Booking>> GetBookingsByCustomerId(int customerId)
        {
            return await _context.Bookings
                .Where(b => b.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetBookingsByRoomId(int roomId)
        {
            return await _context.Bookings
                .Where(b => b.RoomId == roomId)
                .ToListAsync();
        }

        //public async Task<bool> IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut)
        //{
        //    var isBooked = await _context.Bookings
        //        .AnyAsync(b => b.RoomId == roomId &&
        //                       b.Status != BookingStatus.Cancelled &&
        //                       (checkIn < b.CheckOut && checkOut > b.CheckIn));
        //    return !isBooked;
        //}

        
        public async Task<List<Room>> GetRoomsByAmenities(List<int> amenityIds)
        {
            return await _context.Rooms
                .Where(r => r.Amenities.Any(a => amenityIds.Contains(a.Id)))
                .ToListAsync();
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


        public async Task<Payment> ProcessPayment(int bookingId, int amount, string paymentToken)
        {
            // Create a new Payment entity
            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                Status = PaymentStatus.Pending // Initial status
            };

            try
            {
                // Create a PaymentIntent using Stripe
                var options = new PaymentIntentCreateOptions
                {
                    Amount = amount * 100, // Stripe uses cents, so multiply by 100
                    Currency = "zar", // Replace with your currency
                    PaymentMethod = paymentToken, // Token from the frontend
                    Confirm = true, // Automatically confirm the payment
                    OffSession = true, // Payment is happening without the customer being present
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                // Check if the payment was successful
                if (paymentIntent.Status == "succeeded")
                {
                    payment.Status = PaymentStatus.Completed;
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                }
            }
            catch (StripeException ex)
            {
                // Handle Stripe-specific errors
                payment.Status = PaymentStatus.Failed;
                _logger.LogError(ex, "Stripe payment failed for booking {BookingId}", bookingId);
            }
            catch (Exception ex)
            {
                // Handle other errors
                payment.Status = PaymentStatus.Failed;
                _logger.LogError(ex, "Payment processing failed for booking {BookingId}", bookingId);
            }

            // Save the payment to the database
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            return payment;
        }

        

        public async Task<Payment> ProcessPaymentAndUpdateBooking(int bookingId, int amount, string paymentToken)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new ArgumentException("Booking not found.");

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Stripe",
                Status = PaymentStatus.Completed // Assume success for simplicity
            };

            _context.Payments.Add(payment);
            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();

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
