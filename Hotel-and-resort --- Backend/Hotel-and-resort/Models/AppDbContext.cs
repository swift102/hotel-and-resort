using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using Hotel_and_resort.Models;



namespace hotel_and_resort.Models
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Amenities> Amenities { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

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

            modelBuilder.Entity<Booking>()
             .Property(b => b.TotalPrice)
             .HasColumnType("decimal(18,2)");


            // One-to-Many: Room -> Reservation
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Bookings)
                .WithOne(res => res.Room)
                .HasForeignKey(res => res.RoomId);


            modelBuilder.Entity<Room>()
               .HasMany(r => r.Amenities)
               .WithMany(a => a.Rooms);

            // User relationships
            modelBuilder.Entity<User>()
              .Property(u => u.UserProfileID)
              .IsRequired();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserProfileID)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<User>(u => u.UserProfileID);

            // Unique indexes for Customer Email and Phone
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Phone)
                .IsUnique();

            base.OnModelCreating(modelBuilder);

            // Seed roles
            //modelBuilder.Entity<IdentityRole>().HasData(
            //    new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
            //    new IdentityRole { Name = "Staff", NormalizedName = "STAFF" },
            //    new IdentityRole { Name = "Guest", NormalizedName = "GUEST" }
            //);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RefreshToken>()
                .HasKey(t => t.Id);
            modelBuilder.Entity<RefreshToken>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<Room>()
             .Property(r => r.BasePrice)
             .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(t => t.UserId);


            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.UserId)
                .IsUnique();
        }
    }

}


