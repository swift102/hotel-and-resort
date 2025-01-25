using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using hotel_and_resort.Models;

namespace hotel_and_resort.Models
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Amenities> Amenities { get; set; }
        public DbSet<Image> Images { get; set; }
   

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Many-to-Many: Room <-> Amenity
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Amenities)
                .WithMany(a => a.Rooms)
                .UsingEntity<Dictionary<string, object>>(
                    "RoomAmenity",
                    j => j.HasOne<Amenities>().WithMany().HasForeignKey("AmenitiesID"),
                    j => j.HasOne<Room>().WithMany().HasForeignKey("RoomID"));

            // Many-to-Many: Room <-> Booking
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Bookings)
                .WithMany(b => b.Rooms)
                .UsingEntity<Dictionary<string, object>>(
                    "BookingRoom",
                    j => j.HasOne<Booking>().WithMany().HasForeignKey("BookingID"),
                    j => j.HasOne<Room>().WithMany().HasForeignKey("RoomID"));


            // One-to-Many: Room -> Image
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Images)
                .WithOne(i => i.Room)
                .HasForeignKey(i => i.RoomID);

            // One-to-Many: Booking -> Payment
              modelBuilder.Entity<Booking>()
                .HasMany(b => b.Payments)
                .WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId);

            // One-to-Many: Customer -> Booking
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Bookings)
                .WithOne(b => b.Customer)
                .HasForeignKey(b => b.CustomerId);




            // Seed data or additional configurations can be added here if needed
        }
    }

}


