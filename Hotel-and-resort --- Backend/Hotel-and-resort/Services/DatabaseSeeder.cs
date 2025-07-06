using Hotel_and_resort.Data;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hotel_and_resort.Services
{
    public class DatabaseSeeder
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseSeeder(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task SeedAsync()
        {
            try
            {
                // 1. Seed Identity Roles
                await SeedRolesAsync();

                // 2. Seed User Profiles
                if (!_context.UserProfiles.Any())
                {
                    await _context.UserProfiles.AddRangeAsync(SeedData.GetUserProfiles());
                    await _context.SaveChangesAsync();
                }

                // 3. Seed Users with Identity
                await SeedUsersAsync();


                // 5. Seed Customers
                if (!_context.Customers.Any())
                {
                    await _context.Customers.AddRangeAsync(SeedData.GetCustomers());
                    await _context.SaveChangesAsync();
                }

                // 6. Seed Amenities
                if (!_context.Amenities.Any())
                {
                    await _context.Amenities.AddRangeAsync(SeedData.GetAmenities());
                    await _context.SaveChangesAsync();
                }

                // 7. Seed Rooms
                if (!_context.Rooms.Any())
                {
                    await _context.Rooms.AddRangeAsync(SeedData.GetRooms());
                    await _context.SaveChangesAsync();
                }

                // 8. Seed Room-Amenities Relationships
                await SeedRoomAmenitiesAsync();

                // 9. Seed Bookings
                if (!_context.Bookings.Any())
                {
                    await _context.Bookings.AddRangeAsync(SeedData.GetBookings());
                    await _context.SaveChangesAsync();
                }

                // 10. Seed Payments
                if (!_context.Payments.Any())
                {
                    await _context.Payments.AddRangeAsync(SeedData.GetPayments());
                    await _context.SaveChangesAsync();
                }

                // 11. Seed Images
                if (!_context.Images.Any())
                {
                    await _context.Images.AddRangeAsync(SeedData.GetImages());
                    await _context.SaveChangesAsync();
                }

                // 12. Seed Refresh Tokens (optional)
                if (!_context.RefreshTokens.Any())
                {
                    await _context.RefreshTokens.AddRangeAsync(SeedData.GetRefreshTokens());
                    await _context.SaveChangesAsync();
                }

                Console.WriteLine("Serenity Haven Resort database seeded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding database: {ex.Message}");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            var roles = SeedData.GetIdentityRoles();

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role.Name))
                {
                    await _roleManager.CreateAsync(role);
                }
            }
        }

        private async Task SeedUsersAsync()
        {
            var users = SeedData.GetUsers();

            foreach (var user in users)
            {
                if (await _userManager.FindByEmailAsync(user.Email) == null)
                {
                    // Create user with default password
                    var result = await _userManager.CreateAsync(user, "SerenityHaven123!");

                    if (result.Succeeded)
                    {
                        // Assign roles to users
                        string roleName = GetRoleForUser(user.Email);
                        if (!string.IsNullOrEmpty(roleName))
                        {
                            await _userManager.AddToRoleAsync(user, roleName);
                        }
                    }
                }
            }
        }

        private string GetRoleForUser(string email)
        {
            return email switch
            {
                "admin@serenityhaven.com" => "Admin",
                "manager@serenityhaven.com" => "Manager",
                "frontdesk@serenityhaven.com" => "FrontDesk",
                "housekeeping@serenityhaven.com" => "Housekeeping",
                _ => "Guest" // All guest users get Guest role
            };
        }

        private async Task SeedRoomAmenitiesAsync()
        {
            var roomAmenitiesMapping = SeedData.GetRoomAmenitiesMapping();

            foreach (var mapping in roomAmenitiesMapping)
            {
                var room = await _context.Rooms
                    .Include(r => r.Amenities)
                    .FirstOrDefaultAsync(r => r.Id == mapping.Key);

                if (room != null && room.Amenities?.Count == 0)
                {
                    var amenities = await _context.Amenities
                        .Where(a => mapping.Value.Contains(a.Id))
                        .ToListAsync();

                    room.Amenities = amenities;
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}

