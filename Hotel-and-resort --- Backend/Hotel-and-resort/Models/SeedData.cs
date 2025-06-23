using hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;

namespace Hotel_and_resort.Models
{
    public class SeedData
    {
        // 1. IDENTITY ROLES DATA
        public static List<IdentityRole> GetIdentityRoles()
        {
            return new List<IdentityRole>
        {
            new IdentityRole { Id = "role-admin", Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = "role-manager", Name = "Manager", NormalizedName = "MANAGER" },
            new IdentityRole { Id = "role-frontdesk", Name = "FrontDesk", NormalizedName = "FRONTDESK" },
            new IdentityRole { Id = "role-housekeeping", Name = "Housekeeping", NormalizedName = "HOUSEKEEPING" },
            new IdentityRole { Id = "role-guest", Name = "Guest", NormalizedName = "GUEST" }
        };
        }

        // 2. USER PROFILES DATA
        public static List<UserProfile> GetUserProfiles()
        {
            return new List<UserProfile>
        {
            new UserProfile { UserProfileID = 1, ProfileDescription = "System Administrator with full access to resort management" },
            new UserProfile { UserProfileID = 2, ProfileDescription = "Resort Manager responsible for daily operations" },
            new UserProfile { UserProfileID = 3, ProfileDescription = "Front desk staff handling guest check-ins and inquiries" },
            new UserProfile { UserProfileID = 4, ProfileDescription = "Housekeeping supervisor managing room cleaning schedules" },
            new UserProfile { UserProfileID = 5, ProfileDescription = "Guest profile for booking and stay management" },
            new UserProfile { UserProfileID = 6, ProfileDescription = "VIP guest with loyalty program benefits" },
            new UserProfile { UserProfileID = 7, ProfileDescription = "Corporate guest profile for business travelers" },
            new UserProfile { UserProfileID = 8, ProfileDescription = "Family guest profile for vacation stays" },
            new UserProfile { UserProfileID = 9, ProfileDescription = "Regular guest with multiple previous stays" },
            new UserProfile { UserProfileID = 10, ProfileDescription = "New guest first-time visitor" }
        };
        }

        // 3. USERS DATA (Staff + Sample Guests)
        public static List<User> GetUsers()
        {
            return new List<User>
        {
            // Staff Users
            new User
            {
                Id = "admin-001",
                UserName = "admin@serenityhaven.com",
                Email = "admin@serenityhaven.com",
                Name = "Sarah",
                Surname = "Johnson",
                ContactNumber = "+27123456789",
                UserProfileID = 1,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            },
            new User
            {
                Id = "manager-001",
                UserName = "manager@serenityhaven.com",
                Email = "manager@serenityhaven.com",
                Name = "Michael",
                Surname = "Thompson",
                ContactNumber = "+27123456790",
                UserProfileID = 2,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            },
            new User
            {
                Id = "frontdesk-001",
                UserName = "frontdesk@serenityhaven.com",
                Email = "frontdesk@serenityhaven.com",
                Name = "Emily",
                Surname = "Davis",
                ContactNumber = "+27123456791",
                UserProfileID = 3,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            },
            new User
            {
                Id = "housekeeping-001",
                UserName = "housekeeping@serenityhaven.com",
                Email = "housekeeping@serenityhaven.com",
                Name = "Maria",
                Surname = "Rodriguez",
                ContactNumber = "+27123456792",
                UserProfileID = 4,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            },
            // Guest Users
            new User
            {
                Id = "guest-001",
                UserName = "john.smith@email.com",
                Email = "john.smith@email.com",
                Name = "John",
                Surname = "Smith",
                ContactNumber = "+27987654321",
                UserProfileID = 5,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            },
            new User
            {
                Id = "guest-002",
                UserName = "lisa.brown@email.com",
                Email = "lisa.brown@email.com",
                Name = "Lisa",
                Surname = "Brown",
                ContactNumber = "+27987654322",
                UserProfileID = 6,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            },
            new User
            {
                Id = "guest-003",
                UserName = "david.wilson@email.com",
                Email = "david.wilson@email.com",
                Name = "David",
                Surname = "Wilson",
                ContactNumber = "+27987654323",
                UserProfileID = 7,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            },
            new User
            {
                Id = "guest-004",
                UserName = "anna.garcia@email.com",
                Email = "anna.garcia@email.com",
                Name = "Anna",
                Surname = "Garcia",
                ContactNumber = "+27987654324",
                UserProfileID = 8,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            },
            new User
            {
                Id = "guest-005",
                UserName = "robert.taylor@email.com",
                Email = "robert.taylor@email.com",
                Name = "Robert",
                Surname = "Taylor",
                ContactNumber = "+27987654325",
                UserProfileID = 9,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            }
        };
        }

        // 4. CUSTOMERS DATA
        public static List<Customer> GetCustomers()
        {
            return new List<Customer>
        {
            new Customer
            {
                Id = 1,
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@email.com",
                Phone = "+27987654321",
                UserProfileID = 5,
                UserId = "guest-001"
            },
            new Customer
            {
                Id = 2,
                FirstName = "Lisa",
                LastName = "Brown",
                Email = "lisa.brown@email.com",
                Phone = "+27987654322",
                UserProfileID = 6,
                UserId = "guest-002"
            },
            new Customer
            {
                Id = 3,
                FirstName = "David",
                LastName = "Wilson",
                Email = "david.wilson@email.com",
                Phone = "+27987654323",
                UserProfileID = 7,
                UserId = "guest-003"
            },
            new Customer
            {
                Id = 4,
                FirstName = "Anna",
                LastName = "Garcia",
                Email = "anna.garcia@email.com",
                Phone = "+27987654324",
                UserProfileID = 8,
                UserId = "guest-004"
            },
            new Customer
            {
                Id = 5,
                FirstName = "Robert",
                LastName = "Taylor",
                Email = "robert.taylor@email.com",
                Phone = "+27987654325",
                UserProfileID = 9,
                UserId = "guest-005"
            },
            new Customer
            {
                Id = 6,
                FirstName = "Emma",
                LastName = "Johnson",
                Email = "emma.johnson@email.com",
                Phone = "+27987654326",
                UserProfileID = 10,
                UserId = "guest-006"
            },
            new Customer
            {
                Id = 7,
                FirstName = "James",
                LastName = "Anderson",
                Email = "james.anderson@email.com",
                Phone = "+27987654327",
                UserProfileID = 5,
                UserId = "guest-007"
            },
            new Customer
            {
                Id = 8,
                FirstName = "Sophie",
                LastName = "Martinez",
                Email = "sophie.martinez@email.com",
                Phone = "+27987654328",
                UserProfileID = 6,
                UserId = "guest-008"
            }
        };
        }

        // 5. AMENITIES DATA
        public static List<Amenities> GetAmenities()
        {
            return new List<Amenities>
        {
            new Amenities { Id = 1, Name = "WiFi", Description = "High-speed wireless internet access throughout the room" },
            new Amenities { Id = 2, Name = "Air Conditioning", Description = "Climate-controlled environment for optimal comfort" },
            new Amenities { Id = 3, Name = "Mini Bar", Description = "Fully stocked mini refrigerator with beverages and snacks" },
            new Amenities { Id = 4, Name = "Room Service", Description = "24/7 in-room dining service available" },
            new Amenities { Id = 5, Name = "Flat Screen TV", Description = "55-inch LED smart TV with premium channels" },
            new Amenities { Id = 6, Name = "Balcony", Description = "Private balcony with scenic resort views" },
            new Amenities { Id = 7, Name = "Jacuzzi", Description = "Private in-room jacuzzi for ultimate relaxation" },
            new Amenities { Id = 8, Name = "Ocean View", Description = "Panoramic ocean views from room windows" },
            new Amenities { Id = 9, Name = "Safe", Description = "In-room security safe for valuables" },
            new Amenities { Id = 10, Name = "Coffee Machine", Description = "Nespresso coffee machine with premium capsules" },
            new Amenities { Id = 11, Name = "Robes & Slippers", Description = "Luxury bathrobes and comfortable slippers" },
            new Amenities { Id = 12, Name = "Marble Bathroom", Description = "Elegant marble bathroom with premium fixtures" },
            new Amenities { Id = 13, Name = "Butler Service", Description = "Personal butler service for suite guests" },
            new Amenities { Id = 14, Name = "Private Pool", Description = "Exclusive private pool access" },
            new Amenities { Id = 15, Name = "Kitchen", Description = "Fully equipped kitchenette with cooking facilities" }
        };
        }

        // 6. ROOMS DATA
        public static List<Room> GetRooms()
        {
            return new List<Room>
        {
            // Standard Rooms (101-110)
            new Room { Id = 1, Name = "Standard Garden View", Description = "Comfortable room with garden views and essential amenities", PricePerNight = 1200, RoomType = "Standard", Capacity = 2, Features = "Garden View, Queen Bed, Work Desk", Category = "Standard", DynamicPrice = 1200, BasePrice = 1200, RoomNumber = 101, IsAvailable = true },
            new Room { Id = 2, Name = "Standard Garden View", Description = "Comfortable room with garden views and essential amenities", PricePerNight = 1200, RoomType = "Standard", Capacity = 2, Features = "Garden View, Queen Bed, Work Desk", Category = "Standard", DynamicPrice = 1200, BasePrice = 1200, RoomNumber = 102, IsAvailable = true },
            new Room { Id = 3, Name = "Standard Garden View", Description = "Comfortable room with garden views and essential amenities", PricePerNight = 1200, RoomType = "Standard", Capacity = 2, Features = "Garden View, Queen Bed, Work Desk", Category = "Standard", DynamicPrice = 1200, BasePrice = 1200, RoomNumber = 103, IsAvailable = false },
            new Room { Id = 4, Name = "Standard Garden View", Description = "Comfortable room with garden views and essential amenities", PricePerNight = 1200, RoomType = "Standard", Capacity = 2, Features = "Garden View, Queen Bed, Work Desk", Category = "Standard", DynamicPrice = 1200, BasePrice = 1200, RoomNumber = 104, IsAvailable = true },
            new Room { Id = 5, Name = "Standard Garden View", Description = "Comfortable room with garden views and essential amenities", PricePerNight = 1200, RoomType = "Standard", Capacity = 2, Features = "Garden View, Queen Bed, Work Desk", Category = "Standard", DynamicPrice = 1200, BasePrice = 1200, RoomNumber = 105, IsAvailable = true },

            // Deluxe Rooms (201-210)
            new Room { Id = 6, Name = "Deluxe Ocean View", Description = "Spacious room with stunning ocean views and premium amenities", PricePerNight = 2000, RoomType = "Deluxe", Capacity = 3, Features = "Ocean View, King Bed, Seating Area, Mini Bar", Category = "Deluxe", DynamicPrice = 2000, BasePrice = 2000, RoomNumber = 201, IsAvailable = true },
            new Room { Id = 7, Name = "Deluxe Ocean View", Description = "Spacious room with stunning ocean views and premium amenities", PricePerNight = 2000, RoomType = "Deluxe", Capacity = 3, Features = "Ocean View, King Bed, Seating Area, Mini Bar", Category = "Deluxe", DynamicPrice = 2000, BasePrice = 2000, RoomNumber = 202, IsAvailable = true },
            new Room { Id = 8, Name = "Deluxe Ocean View", Description = "Spacious room with stunning ocean views and premium amenities", PricePerNight = 2000, RoomType = "Deluxe", Capacity = 3, Features = "Ocean View, King Bed, Seating Area, Mini Bar", Category = "Deluxe", DynamicPrice = 2200, BasePrice = 2000, RoomNumber = 203, IsAvailable = false },
            new Room { Id = 9, Name = "Deluxe Ocean View", Description = "Spacious room with stunning ocean views and premium amenities", PricePerNight = 2000, RoomType = "Deluxe", Capacity = 3, Features = "Ocean View, King Bed, Seating Area, Mini Bar", Category = "Deluxe", DynamicPrice = 2000, BasePrice = 2000, RoomNumber = 204, IsAvailable = true },
            new Room { Id = 10, Name = "Deluxe Ocean View", Description = "Spacious room with stunning ocean views and premium amenities", PricePerNight = 2000, RoomType = "Deluxe", Capacity = 3, Features = "Ocean View, King Bed, Seating Area, Mini Bar", Category = "Deluxe", DynamicPrice = 2000, BasePrice = 2000, RoomNumber = 205, IsAvailable = true },

            // Junior Suites (301-305)
            new Room { Id = 11, Name = "Junior Suite", Description = "Elegant suite with separate living area and luxury amenities", PricePerNight = 3500, RoomType = "Suite", Capacity = 4, Features = "Ocean View, King Bed, Living Area, Balcony, Jacuzzi", Category = "Suite", DynamicPrice = 3500, BasePrice = 3500, RoomNumber = 301, IsAvailable = true },
            new Room { Id = 12, Name = "Junior Suite", Description = "Elegant suite with separate living area and luxury amenities", PricePerNight = 3500, RoomType = "Suite", Capacity = 4, Features = "Ocean View, King Bed, Living Area, Balcony, Jacuzzi", Category = "Suite", DynamicPrice = 3800, BasePrice = 3500, RoomNumber = 302, IsAvailable = true },
            new Room { Id = 13, Name = "Junior Suite", Description = "Elegant suite with separate living area and luxury amenities", PricePerNight = 3500, RoomType = "Suite", Capacity = 4, Features = "Ocean View, King Bed, Living Area, Balcony, Jacuzzi", Category = "Suite", DynamicPrice = 3500, BasePrice = 3500, RoomNumber = 303, IsAvailable = false },
            new Room { Id = 14, Name = "Junior Suite", Description = "Elegant suite with separate living area and luxury amenities", PricePerNight = 3500, RoomType = "Suite", Capacity = 4, Features = "Ocean View, King Bed, Living Area, Balcony, Jacuzzi", Category = "Suite", DynamicPrice = 3500, BasePrice = 3500, RoomNumber = 304, IsAvailable = true },
            new Room { Id = 15, Name = "Junior Suite", Description = "Elegant suite with separate living area and luxury amenities", PricePerNight = 3500, RoomType = "Suite", Capacity = 4, Features = "Ocean View, King Bed, Living Area, Balcony, Jacuzzi", Category = "Suite", DynamicPrice = 3500, BasePrice = 3500, RoomNumber = 305, IsAvailable = true },

            // Presidential Suites (401-403)
            new Room { Id = 16, Name = "Presidential Suite", Description = "Ultimate luxury suite with private pool and butler service", PricePerNight = 8000, RoomType = "Presidential", Capacity = 6, Features = "Private Pool, Butler Service, Multiple Bedrooms, Kitchen, Dining Room", Category = "Presidential", DynamicPrice = 8000, BasePrice = 8000, RoomNumber = 401, IsAvailable = true },
            new Room { Id = 17, Name = "Presidential Suite", Description = "Ultimate luxury suite with private pool and butler service", PricePerNight = 8000, RoomType = "Presidential", Capacity = 6, Features = "Private Pool, Butler Service, Multiple Bedrooms, Kitchen, Dining Room", Category = "Presidential", DynamicPrice = 8500, BasePrice = 8000, RoomNumber = 402, IsAvailable = true },
            new Room { Id = 18, Name = "Presidential Suite", Description = "Ultimate luxury suite with private pool and butler service", PricePerNight = 8000, RoomType = "Presidential", Capacity = 6, Features = "Private Pool, Butler Service, Multiple Bedrooms, Kitchen, Dining Room", Category = "Presidential", DynamicPrice = 8000, BasePrice = 8000, RoomNumber = 403, IsAvailable = true },

            // Family Rooms (501-506)
            new Room { Id = 19, Name = "Family Room", Description = "Spacious family accommodation with connecting rooms", PricePerNight = 2800, RoomType = "Family", Capacity = 5, Features = "Connecting Rooms, Two Bathrooms, Kitchenette, Garden View", Category = "Family", DynamicPrice = 2800, BasePrice = 2800, RoomNumber = 501, IsAvailable = true },
            new Room { Id = 20, Name = "Family Room", Description = "Spacious family accommodation with connecting rooms", PricePerNight = 2800, RoomType = "Family", Capacity = 5, Features = "Connecting Rooms, Two Bathrooms, Kitchenette, Garden View", Category = "Family", DynamicPrice = 3000, BasePrice = 2800, RoomNumber = 502, IsAvailable = true }
        };
        }

        // 7. BOOKINGS DATA
        public static List<Booking> GetBookings()
        {
            var now = DateTime.Now;
            return new List<Booking>
        {
            // Current/Future Bookings
            new Booking
            {
                Id = 1, RoomId = 3, UserId = "guest-001", UserProfileID = 5, CustomerId = 1,
                CheckIn = now.AddDays(5), CheckOut = now.AddDays(8), TotalPrice = 3600,
                CreatedAt = now.AddDays(-2), Status = BookingStatus.Confirmed, IsRefundable = true,
                GuestCount = 2, RefundPercentage = 80, PaymentIntentId = "pi_test_001"
            },
            new Booking
            {
                Id = 2, RoomId = 8, UserId = "guest-002", UserProfileID = 6, CustomerId = 2,
                CheckIn = now.AddDays(10), CheckOut = now.AddDays(14), TotalPrice = 8800,
                CreatedAt = now.AddDays(-5), Status = BookingStatus.Confirmed, IsRefundable = true,
                GuestCount = 2, RefundPercentage = 100, PaymentIntentId = "pi_test_002"
            },
            new Booking
            {
                Id = 3, RoomId = 13, UserId = "guest-003", UserProfileID = 7, CustomerId = 3,
                CheckIn = now.AddDays(15), CheckOut = now.AddDays(18), TotalPrice = 10500,
                CreatedAt = now.AddDays(-1), Status = BookingStatus.Confirmed, IsRefundable = true,
                GuestCount = 3, RefundPercentage = 90, PaymentIntentId = "pi_test_003"
            },
            
            // Pending Bookings
            new Booking
            {
                Id = 4, RoomId = 6, UserId = "guest-004", UserProfileID = 8, CustomerId = 4,
                CheckIn = now.AddDays(20), CheckOut = now.AddDays(23), TotalPrice = 6000,
                CreatedAt = now.AddHours(-2), Status = BookingStatus.Pending, IsRefundable = true,
                GuestCount = 2, RefundPercentage = 100, PaymentIntentId = "pi_test_004"
            },
            
            // Historical Completed Bookings
            new Booking
            {
                Id = 5, RoomId = 1, UserId = "guest-005", UserProfileID = 9, CustomerId = 5,
                CheckIn = now.AddDays(-20), CheckOut = now.AddDays(-17), TotalPrice = 3600,
                CreatedAt = now.AddDays(-25), Status = BookingStatus.Completed, IsRefundable = false,
                GuestCount = 2, RefundPercentage = 0, PaymentIntentId = "pi_test_005"
            },
            new Booking
            {
                Id = 6, RoomId = 11, UserId = "guest-002", UserProfileID = 6, CustomerId = 2,
                CheckIn = now.AddDays(-15), CheckOut = now.AddDays(-12), TotalPrice = 10500,
                CreatedAt = now.AddDays(-20), Status = BookingStatus.Completed, IsRefundable = false,
                GuestCount = 3, RefundPercentage = 0, PaymentIntentId = "pi_test_006"
            },
            
            // Cancelled Booking
            new Booking
            {
                Id = 7, RoomId = 7, UserId = "guest-001", UserProfileID = 5, CustomerId = 1,
                CheckIn = now.AddDays(-5), CheckOut = now.AddDays(-2), TotalPrice = 6000,
                CreatedAt = now.AddDays(-10), CancelledAt = now.AddDays(-7), Status = BookingStatus.Cancelled, IsRefundable = true,
                GuestCount = 2, RefundPercentage = 80, PaymentIntentId = "pi_test_007"
            }
        };
        }

        // 8. PAYMENTS DATA
        public static List<Payment> GetPayments()
        {
            var now = DateTime.Now;
            return new List<Payment>
        {
            new Payment
            {
                Id = 1, BookingId = 1, Amount = 3600, PaymentDate = now.AddDays(-2),
                PaymentMethod = "Stripe", Status = PaymentStatus.Completed,
                StatusMessage = "Payment successful", TransactionId = "txn_001",
                Currency = "ZAR", CustomerId = 1, StripePaymentIntentId = "pi_test_001",
                CreatedAt = now.AddDays(-2)
            },
            new Payment
            {
                Id = 2, BookingId = 2, Amount = 8800, PaymentDate = now.AddDays(-5),
                PaymentMethod = "PayFast", Status = PaymentStatus.Completed,
                StatusMessage = "Payment processed successfully", TransactionId = "txn_002",
                Currency = "ZAR", CustomerId = 2, CreatedAt = now.AddDays(-5)
            },
            new Payment
            {
                Id = 3, BookingId = 3, Amount = 10500, PaymentDate = now.AddDays(-1),
                PaymentMethod = "Stripe", Status = PaymentStatus.Completed,
                StatusMessage = "Payment confirmed", TransactionId = "txn_003",
                Currency = "ZAR", CustomerId = 3, StripePaymentIntentId = "pi_test_003",
                CreatedAt = now.AddDays(-1)
            },
            new Payment
            {
                Id = 4, BookingId = 4, Amount = 6000, PaymentDate = now.AddHours(-2),
                PaymentMethod = "Stripe", Status = PaymentStatus.Pending,
                StatusMessage = "Payment processing", TransactionId = "txn_004",
                Currency = "ZAR", CustomerId = 4, StripePaymentIntentId = "pi_test_004",
                CreatedAt = now.AddHours(-2)
            },
            new Payment
            {
                Id = 5, BookingId = 5, Amount = 3600, PaymentDate = now.AddDays(-20),
                PaymentMethod = "PayFast", Status = PaymentStatus.Completed,
                StatusMessage = "Historical payment completed", TransactionId = "txn_005",
                Currency = "ZAR", CustomerId = 5, CreatedAt = now.AddDays(-20)
            },
            new Payment
            {
                Id = 6, BookingId = 6, Amount = 10500, PaymentDate = now.AddDays(-15),
                PaymentMethod = "Stripe", Status = PaymentStatus.Completed,
                StatusMessage = "Suite booking payment", TransactionId = "txn_006",
                Currency = "ZAR", CustomerId = 2, StripePaymentIntentId = "pi_test_006",
                CreatedAt = now.AddDays(-15)
            },
            new Payment
            {
                Id = 7, BookingId = 7, Amount = 4800, PaymentDate = now.AddDays(-7),
                PaymentMethod = "Stripe", Status = PaymentStatus.Refunded,
                StatusMessage = "Refund processed for cancellation", TransactionId = "txn_007",
                Currency = "ZAR", CustomerId = 1, StripePaymentIntentId = "pi_test_007",
                CreatedAt = now.AddDays(-7)
            }
        };
        }

        // 9. IMAGES DATA
        public static List<Image> GetImages()
        {
            return new List<Image>
    {
        // Standard Room Images
        new Image { Id = 1, Name = "Standard Room Main", ImagePath = "/images/rooms/standard/room-101-main.jpg", RoomID = 1 },
        new Image { Id = 2, Name = "Standard Room Bathroom", ImagePath = "/images/rooms/standard/room-101-bathroom.jpg", RoomID = 1 },
        new Image { Id = 3, Name = "Standard Room View", ImagePath = "/images/rooms/standard/room-102-view.jpg", RoomID = 2 },
        new Image { Id = 4, Name = "Standard Room Amenities", ImagePath = "/images/rooms/standard/room-103-amenities.jpg", RoomID = 3 },

        // Deluxe Room Images
        new Image { Id = 5, Name = "Deluxe Ocean View Main", ImagePath = "/images/rooms/deluxe/room-201-main.jpg", RoomID = 6 },
        new Image { Id = 6, Name = "Deluxe Ocean View Balcony", ImagePath = "/images/rooms/deluxe/room-201-balcony.jpg", RoomID = 6 },
        new Image { Id = 7, Name = "Deluxe Ocean View Bathroom", ImagePath = "/images/rooms/deluxe/room-202-bathroom.jpg", RoomID = 7 },
        new Image { Id = 8, Name = "Deluxe Seating Area", ImagePath = "/images/rooms/deluxe/room-203-seating.jpg", RoomID = 8 },

        // Suite Images
        new Image { Id = 9, Name = "Junior Suite Living Area", ImagePath = "/images/rooms/suite/suite-301-living.jpg", RoomID = 11 },
        new Image { Id = 10, Name = "Junior Suite Bedroom", ImagePath = "/images/rooms/suite/suite-301-bedroom.jpg", RoomID = 11 },
        new Image { Id = 11, Name = "Junior Suite Jacuzzi", ImagePath = "/images/rooms/suite/suite-302-jacuzzi.jpg", RoomID = 12 },
        new Image { Id = 12, Name = "Junior Suite Balcony", ImagePath = "/images/rooms/suite/suite-303-balcony.jpg", RoomID = 13 },

        // Presidential Suite Images
        new Image { Id = 13, Name = "Presidential Suite Main", ImagePath = "/images/rooms/presidential/suite-401-main.jpg", RoomID = 16 },
        new Image { Id = 14, Name = "Presidential Suite Pool", ImagePath = "/images/rooms/presidential/suite-401-pool.jpg", RoomID = 16 },
        new Image { Id = 15, Name = "Presidential Suite Dining", ImagePath = "/images/rooms/presidential/suite-402-dining.jpg", RoomID = 17 },
        new Image { Id = 16, Name = "Presidential Suite Kitchen", ImagePath = "/images/rooms/presidential/suite-403-kitchen.jpg", RoomID = 18 },

        // Family Room Images
        new Image { Id = 17, Name = "Family Room Main", ImagePath = "/images/rooms/family/room-501-main.jpg", RoomID = 19 },
        new Image { Id = 18, Name = "Family Room Kids Area", ImagePath = "/images/rooms/family/room-502-kids.jpg", RoomID = 20 }
    };
        }

        // 10. REFRESH TOKENS DATA (Optional - for testing)
        public static List<RefreshToken> GetRefreshTokens()
        {
            return new List<RefreshToken>
        {
            new RefreshToken
            {
                Id = 1,
                UserId = "guest-001",
                Token = "refresh_token_guest_001_example",
                ExpiryDate = DateTime.Now.AddDays(30)
            },
            new RefreshToken
            {
                Id = 2,
                UserId = "guest-002",
                Token = "refresh_token_guest_002_example",
                ExpiryDate = DateTime.Now.AddDays(30)
            },
            new RefreshToken
            {
                Id = 3,
                UserId = "admin-001",
                Token = "refresh_token_admin_001_example",
                ExpiryDate = DateTime.Now.AddDays(30)
            }
        };
        }

        // 11. ROOM-AMENITIES MANY-TO-MANY RELATIONSHIPS
        public static Dictionary<int, List<int>> GetRoomAmenitiesMapping()
        {
            return new Dictionary<int, List<int>>
        {
            // Standard Rooms (Basic Amenities)
            { 1, new List<int> { 1, 2, 5, 9, 10 } }, // WiFi, AC, TV, Safe, Coffee
            { 2, new List<int> { 1, 2, 5, 9, 10 } },
            { 3, new List<int> { 1, 2, 5, 9, 10 } },
            { 4, new List<int> { 1, 2, 5, 9, 10 } },
            { 5, new List<int> { 1, 2, 5, 9, 10 } },

            // Deluxe Rooms (Premium Amenities)
            { 6, new List<int> { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11 } }, // + Mini Bar, Room Service, Balcony, Ocean View, Robes
            { 7, new List<int> { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11 } },
            { 8, new List<int> { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11 } },
            { 9, new List<int> { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11 } },
            { 10, new List<int> { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11 } },

            // Junior Suites (Luxury Amenities)
            { 11, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } }, // + Jacuzzi, Marble Bathroom
            { 12, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } },
            { 13, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } },
            { 14, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } },
            { 15, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } },

            // Presidential Suites (All Amenities)
            { 16, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 } }, // All amenities
            { 17, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 } },
            { 18, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 } },

            // Family Rooms (Family-Focused Amenities)
            { 19, new List<int> { 1, 2, 3, 4, 5, 9, 10, 15 } }, // WiFi, AC, Mini Bar, Room Service, TV, Safe, Coffee, Kitchen
            { 20, new List<int> { 1, 2, 3, 4, 5, 9, 10, 15 } }
        };
        
    }
}
}
