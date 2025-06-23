using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;


namespace Hotel_and_resort.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context, UserManager<User> userManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (context.Users.Any() || context.Rooms.Any())
            {
                return; // Database already seeded
            }

            // Seed Users
            var user1 = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin",
                Email = "admin@hotel.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user1, "Admin@123");
            await userManager.AddToRoleAsync(user1, "Admin");

            var user2 = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "guest",
                Email = "guest@hotel.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user2, "Guest@123");
            await userManager.AddToRoleAsync(user2, "Customer");

           
        }
    }
}
