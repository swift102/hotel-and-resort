using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Hotel_and_resort.Models;
using hotel_and_resort.Models;

namespace Hotel_and_resort.Data
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
            base.OnModelCreating(modelBuilder);

            // Configure table names
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Room>().ToTable("Rooms");
            modelBuilder.Entity<Booking>().ToTable("Bookings");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            modelBuilder.Entity<Amenities>().ToTable("Amenities");
            modelBuilder.Entity<Image>().ToTable("Images");
            modelBuilder.Entity<UserProfile>().ToTable("UserProfiles");
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");

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
                .HasForeignKey(i => i.RoomID)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: Booking -> Payment
            modelBuilder.Entity<Booking>()
                .HasMany(b => b.Payments)
                .WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Customer -> Booking
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Bookings)
                .WithOne(b => b.Customer)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Room -> Booking
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Bookings)
                .WithOne(res => res.Room)
                .HasForeignKey(res => res.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Unique indexes for Customer
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Phone)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<RefreshToken>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(t => t.UserId);

            // Decimal precision configurations
            modelBuilder.Entity<Room>()
                .Property(r => r.BasePrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Room>()
                .Property(r => r.DynamicPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Room>()
                .Property(r => r.PricePerNight)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Booking>()
               .Property(b => b.RefundPercentage)
               .HasPrecision(5, 2);

            // Default values
            modelBuilder.Entity<Booking>()
                .Property(b => b.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Payment>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Check constraints
            modelBuilder.Entity<Room>()
                .HasCheckConstraint("CK_Room_BasePrice", "BasePrice > 0");

            modelBuilder.Entity<Room>()
                .HasCheckConstraint("CK_Room_Capacity", "Capacity > 0");

            modelBuilder.Entity<Payment>()
                .HasCheckConstraint("CK_Payment_Amount", "Amount > 0");

            // Configure enum conversions
            modelBuilder.Entity<Booking>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Payment>()
                .Property(e => e.Status)
                .HasConversion<string>();

        }

      

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Additional configuration if needed
        }
    }
}