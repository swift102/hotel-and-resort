using hotel_and_resort.Models;
using Hotel_and_resort.Data;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hotel_and_resort.Data
{
        public static class DbInitializer
        {
            public static async Task InitializeAsync(
                AppDbContext context,
                UserManager<User> userManager,
                RoleManager<IdentityRole> roleManager,
                ILogger? logger = null)
            {
                try
                {
                    logger?.LogInformation("Starting database initialization...");

                    // Use MigrateAsync instead of EnsureCreatedAsync for better migration support
                    await context.Database.MigrateAsync();

                    // Check if data already exists (check multiple tables for thoroughness)
                    if (await context.Users.AnyAsync() || await context.Rooms.AnyAsync() || await context.Roles.AnyAsync())
                    {
                        logger?.LogInformation("Database already seeded. Skipping initialization.");
                        return;
                    }

                    logger?.LogInformation("Starting database seeding...");

                    // 1. Create Roles first
                    await SeedRolesAsync(roleManager, logger);

                    // 2. Create User Profiles
                    await SeedUserProfilesAsync(context, logger);

                    // 3. Create Users with proper password hashing
                    await SeedUsersAsync(userManager, context, logger);

                    // 4. Assign roles to users
                    await SeedUserRolesAsync(userManager, logger);

                    // 5. Create Customers
                    await SeedCustomersAsync(context, logger);

                    // 6. Create Amenities
                    await SeedAmenitiesAsync(context, logger);

                    // 7. Create Rooms
                    await SeedRoomsAsync(context, logger);

                    // 8. Create Room-Amenity relationships
                    await SeedRoomAmenitiesAsync(context, logger);

                    // 9. Create Images
                    await SeedImagesAsync(context, logger);

                    // 10. Create Bookings
                    await SeedBookingsAsync(context, logger);

                    // 11. Create Payments
                    await SeedPaymentsAsync(context, logger);

                    // 12. Create Refresh Tokens
                    await SeedRefreshTokensAsync(context, logger);

                    logger?.LogInformation("Database seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error during database seeding: {Message}", ex.Message);
                    throw;
                }
            }

            private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding roles...");
                var roles = SeedData.GetIdentityRoles();

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role.Name))
                    {
                        var result = await roleManager.CreateAsync(role);
                        if (result.Succeeded)
                        {
                            logger?.LogInformation("Created role: {RoleName}", role.Name);
                        }
                        else
                        {
                            logger?.LogWarning("Failed to create role {RoleName}: {Errors}",
                                role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        logger?.LogInformation("Role {RoleName} already exists", role.Name);
                    }
                }
            }

            private static async Task SeedUserProfilesAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding user profiles...");
                var userProfiles = SeedData.GetUserProfiles();

                // Check if any profiles already exist
                var existingProfileIds = await context.UserProfiles
                    .Select(up => up.UserProfileID)
                    .ToListAsync();

                var newProfiles = userProfiles.Where(up => !existingProfileIds.Contains(up.UserProfileID)).ToList();

                if (newProfiles.Any())
                {
                    await context.UserProfiles.AddRangeAsync(newProfiles);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Added {Count} user profiles", newProfiles.Count);
                }
                else
                {
                    logger?.LogInformation("No new user profiles to add");
                }
            }

            private static async Task SeedUsersAsync(UserManager<User> userManager, AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding users...");
                var users = SeedData.GetUsers();

                foreach (var user in users)
                {
                    // Check if user profile exists
                    var userProfile = await context.UserProfiles.FindAsync(user.UserProfileID);
                    if (userProfile == null)
                    {
                        logger?.LogWarning("UserProfile {UserProfileId} not found for user {Email}",
                            user.UserProfileID, user.Email);
                        continue;
                    }

                    // Check if user already exists
                    var existingUser = await userManager.FindByEmailAsync(user.Email);
                    if (existingUser == null)
                    {
                        // Create user with default password
                        var result = await userManager.CreateAsync(user, "SerenityHaven123!");
                        if (result.Succeeded)
                        {
                            logger?.LogInformation("Created user: {Email}", user.Email);
                        }
                        else
                        {
                            logger?.LogWarning("Failed to create user {Email}: {Errors}",
                                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        logger?.LogInformation("User {Email} already exists", user.Email);
                    }
                }
            }

            private static async Task SeedUserRolesAsync(UserManager<User> userManager, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding user roles...");
                var userRoleMapping = new Dictionary<string, string>
            {
                { "admin@serenityhaven.com", "Admin" },
                { "manager@serenityhaven.com", "Manager" },
                { "frontdesk@serenityhaven.com", "FrontDesk" },
                { "housekeeping@serenityhaven.com", "Housekeeping" },
                { "john.smith@email.com", "Guest" },
                { "lisa.brown@email.com", "Guest" },
                { "david.wilson@email.com", "Guest" },
                { "anna.garcia@email.com", "Guest" },
                { "robert.taylor@email.com", "Guest" }
            };

                foreach (var mapping in userRoleMapping)
                {
                    var user = await userManager.FindByEmailAsync(mapping.Key);
                    if (user != null)
                    {
                        if (!await userManager.IsInRoleAsync(user, mapping.Value))
                        {
                            var result = await userManager.AddToRoleAsync(user, mapping.Value);
                            if (result.Succeeded)
                            {
                                logger?.LogInformation("Added user {Email} to role {Role}", mapping.Key, mapping.Value);
                            }
                            else
                            {
                                logger?.LogWarning("Failed to add user {Email} to role {Role}: {Errors}",
                                    mapping.Key, mapping.Value, string.Join(", ", result.Errors.Select(e => e.Description)));
                            }
                        }
                        else
                        {
                            logger?.LogInformation("User {Email} already has role {Role}", mapping.Key, mapping.Value);
                        }
                    }
                    else
                    {
                        logger?.LogWarning("User {Email} not found for role assignment", mapping.Key);
                    }
                }
            }

            private static async Task SeedCustomersAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding customers...");
                var customers = SeedData.GetCustomers();

                var existingCustomerIds = await context.Customers
                    .Select(c => c.Id)
                    .ToListAsync();

                var newCustomers = customers.Where(c => !existingCustomerIds.Contains(c.Id)).ToList();

                if (newCustomers.Any())
                {
                    await context.Customers.AddRangeAsync(newCustomers);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Added {Count} customers", newCustomers.Count);
                }
                else
                {
                    logger?.LogInformation("No new customers to add");
                }
            }

            private static async Task SeedAmenitiesAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding amenities...");
                var amenities = SeedData.GetAmenities();

                var existingAmenityIds = await context.Amenities
                    .Select(a => a.Id)
                    .ToListAsync();

                var newAmenities = amenities.Where(a => !existingAmenityIds.Contains(a.Id)).ToList();

                if (newAmenities.Any())
                {
                    await context.Amenities.AddRangeAsync(newAmenities);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Added {Count} amenities", newAmenities.Count);
                }
                else
                {
                    logger?.LogInformation("No new amenities to add");
                }
            }

            private static async Task SeedRoomsAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding rooms...");
                var rooms = SeedData.GetRooms();

                var existingRoomIds = await context.Rooms
                    .Select(r => r.Id)
                    .ToListAsync();

                var newRooms = rooms.Where(r => !existingRoomIds.Contains(r.Id)).ToList();

                if (newRooms.Any())
                {
                    await context.Rooms.AddRangeAsync(newRooms);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Added {Count} rooms", newRooms.Count);
                }
                else
                {
                    logger?.LogInformation("No new rooms to add");
                }
            }

            private static async Task SeedRoomAmenitiesAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding room amenities relationships...");
                var roomAmenitiesMapping = SeedData.GetRoomAmenitiesMapping();

                foreach (var mapping in roomAmenitiesMapping)
                {
                    var room = await context.Rooms.Include(r => r.Amenities)
                        .FirstOrDefaultAsync(r => r.Id == mapping.Key);

                    if (room != null)
                    {
                        foreach (var amenityId in mapping.Value)
                        {
                            var amenity = await context.Amenities.FindAsync(amenityId);
                            if (amenity != null && !room.Amenities.Contains(amenity))
                            {
                                room.Amenities.Add(amenity);
                            }
                        }
                    }
                    else
                    {
                        logger?.LogWarning("Room with ID {RoomId} not found for amenity mapping", mapping.Key);
                    }
                }

                await context.SaveChangesAsync();
                logger?.LogInformation("Room amenities relationships seeded");
            }

            private static async Task SeedImagesAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding images...");
                var images = SeedData.GetImages();

                var existingImageIds = await context.Images
                    .Select(i => i.Id)
                    .ToListAsync();

                var newImages = images.Where(i => !existingImageIds.Contains(i.Id)).ToList();

                if (newImages.Any())
                {
                    await context.Images.AddRangeAsync(newImages);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Added {Count} images", newImages.Count);
                }
                else
                {
                    logger?.LogInformation("No new images to add");
                }
            }

            private static async Task SeedBookingsAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding bookings...");
                var bookings = SeedData.GetBookings();

                var existingBookingIds = await context.Bookings
                    .Select(b => b.Id)
                    .ToListAsync();

                var newBookings = bookings.Where(b => !existingBookingIds.Contains(b.Id)).ToList();

                if (newBookings.Any())
                {
                    await context.Bookings.AddRangeAsync(newBookings);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Added {Count} bookings", newBookings.Count);
                }
                else
                {
                    logger?.LogInformation("No new bookings to add");
                }
            }

            private static async Task SeedPaymentsAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding payments...");
                var payments = SeedData.GetPayments();

                var existingPaymentIds = await context.Payments
                    .Select(p => p.Id)
                    .ToListAsync();

                var newPayments = payments.Where(p => !existingPaymentIds.Contains(p.Id)).ToList();

                if (newPayments.Any())
                {
                    await context.Payments.AddRangeAsync(newPayments);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Added {Count} payments", newPayments.Count);
                }
                else
                {
                    logger?.LogInformation("No new payments to add");
                }
            }

            private static async Task SeedRefreshTokensAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogInformation("Seeding refresh tokens...");
                var refreshTokens = SeedData.GetRefreshTokens();

                var existingTokenIds = await context.RefreshTokens
                    .Select(rt => rt.Id)
                    .ToListAsync();

                var newTokens = refreshTokens.Where(rt => !existingTokenIds.Contains(rt.Id)).ToList();

                if (newTokens.Any())
                {
                    await context.RefreshTokens.AddRangeAsync(newTokens);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Added {Count} refresh tokens", newTokens.Count);
                }
                else
                {
                    logger?.LogInformation("No new refresh tokens to add");
                }
            }

            // Additional helper method to reset database (for development/testing)
            public static async Task ResetDatabaseAsync(AppDbContext context, ILogger? logger = null)
            {
                logger?.LogWarning("Resetting database...");
                await context.Database.EnsureDeletedAsync();
                await context.Database.MigrateAsync();
                logger?.LogInformation("Database reset completed");
            }

            // Method to check database health
            public static async Task<bool> CheckDatabaseHealthAsync(AppDbContext context, ILogger? logger = null)
            {
                try
                {
                    logger?.LogInformation("Checking database health...");
                    await context.Database.CanConnectAsync();

                    var userCount = await context.Users.CountAsync();
                    var roomCount = await context.Rooms.CountAsync();
                    var bookingCount = await context.Bookings.CountAsync();

                    logger?.LogInformation("Database health check passed. Users: {UserCount}, Rooms: {RoomCount}, Bookings: {BookingCount}",
                        userCount, roomCount, bookingCount);

                    return true;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Database health check failed");
                    return false;
                }
            }
        }
}
