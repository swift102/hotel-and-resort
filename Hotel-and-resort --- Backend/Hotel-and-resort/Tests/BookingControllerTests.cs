using hotel_and_resort.Controllers;
using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;



namespace HotelAndResort.Tests.Controllers
{
    public class BookingControllerTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IRepository> _repositoryMock;
        private readonly BookingController _controller;
        private readonly NullLogger<BookingController> _logger;

        public BookingControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB per test
                .Options;
            _context = new AppDbContext(options);

            // Setup repository mock
            _repositoryMock = new Mock<IRepository>();

            // Setup logger
            _logger = new NullLogger<BookingController>();

            // Initialize controller
            _controller = new BookingController(_context, _repositoryMock.Object, _logger);
        }

        [Fact]
        public async Task GetAvailableRooms_ValidDates_ReturnsAvailableRooms()
        {
            // Arrange
            _context.Rooms.AddRange(new[]
            {
                new Room { Id = 1, Name = "Room 1", Price = 100, Capacity = 2, IsAvailable = true },
                new Room { Id = 2, Name = "Room 2", Price = 150, Capacity = 4, IsAvailable = true }
            });
            _context.SaveChanges();

            var checkIn = DateTime.Today.AddDays(1);
            var checkOut = DateTime.Today.AddDays(3);

            // Act
            var result = await _controller.GetAvailableRooms(checkIn, checkOut);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var rooms = Assert.IsAssignableFrom<IEnumerable<RoomReadDTO>>(okResult.Value);
            Assert.Equal(2, rooms.Count());
            Assert.Contains(rooms, r => r.Name == "Room 1");
            Assert.Contains(rooms, r => r.Name == "Room 2");
        }

        [Fact]
        public async Task GetAvailableRooms_BookedRoom_ExcludesBookedRoom()
        {
            // Arrange
            _context.Rooms.AddRange(new[]
            {
                new Room { Id = 1, Name = "Room 1", Price = 100, Capacity = 2, IsAvailable = true },
                new Room { Id = 2, Name = "Room 2", Price = 150, Capacity = 4, IsAvailable = true }
            });
            _context.Bookings.Add(new Booking
            {
                Id = 1,
                RoomId = 1,
                CheckIn = DateTime.Today.AddDays(1),
                CheckOut = DateTime.Today.AddDays(3),
                Status = BookingStatus.Confirmed
            });
            _context.SaveChanges();

            var checkIn = DateTime.Today.AddDays(1);
            var checkOut = DateTime.Today.AddDays(3);

            // Act
            var result = await _controller.GetAvailableRooms(checkIn, checkOut);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var rooms = Assert.IsAssignableFrom<IEnumerable<RoomReadDTO>>(okResult.Value);
            Assert.Single(rooms);
            Assert.Equal("Room 2", rooms.First().Name);
        }

        [Fact]
        public async Task GetAvailableRooms_InvalidDates_ReturnsBadRequest()
        {
            // Arrange
            var checkIn = DateTime.Today.AddDays(-1); // Past date
            var checkOut = DateTime.Today.AddDays(1);

            // Act
            var result = await _controller.GetAvailableRooms(checkIn, checkOut);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid date range. Check-in must be today or later, and check-out must be after check-in.", badRequestResult.Value.GetType().GetProperty("Error")?.GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task CreateBooking_ValidInput_CreatesBooking()
        {
            // Arrange
            _context.Rooms.Add(new Room { Id = 1, Name = "Room 1", Price = 100, Capacity = 2, IsAvailable = true });
            _context.SaveChanges();

            var bookingDto = new BookingCreateDTO
            {
                RoomId = 1,
                CustomerEmail = "test@example.com",
                CustomerFirstName = "John",
                CustomerLastName = "Doe",
                CustomerPhone = "+12345678901",
                CustomerTitle = "Mr",
                CheckIn = DateTime.Today.AddDays(1),
                CheckOut = DateTime.Today.AddDays(3),
                IsRefundable = true
            };

            _repositoryMock.Setup(r => r.IsRoomAvailable(1, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(true);
            _repositoryMock.Setup(r => r.AddCustomer(It.IsAny<Customer>())).ReturnsAsync(new Customer { Id = 1, Email = "test@example.com" });
            _repositoryMock.Setup(r => r.AddBooking(It.IsAny<Booking>())).ReturnsAsync((Booking b) => b);

            // Act
            var result = await _controller.CreateBooking(bookingDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var booking = Assert.IsType<Booking>(createdResult.Value);
            Assert.Equal(1, booking.RoomId);
            Assert.Equal(200, booking.TotalPrice); // 100 * 2 nights
            Assert.Equal(BookingStatus.Pending, booking.Status);
        }

        [Fact]
        public async Task CreateBooking_RoomNotAvailable_ReturnsBadRequest()
        {
            // Arrange
            var bookingDto = new BookingCreateDTO
            {
                RoomId = 1,
                CustomerEmail = "test@example.com",
                CustomerFirstName = "John",
                CustomerLastName = "Doe",
                CustomerPhone = "+12345678901",
                CheckIn = DateTime.Today.AddDays(1),
                CheckOut = DateTime.Today.AddDays(3)
            };

            _repositoryMock.Setup(r => r.IsRoomAvailable(1, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(false);

            // Act
            var result = await _controller.CreateBooking(bookingDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("The selected room is not available for the specified dates.", badRequestResult.Value.GetType().GetProperty("Error")?.GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task GetBooking_ExistingId_ReturnsBooking()
        {
            // Arrange
            var booking = new Booking { Id = 1, RoomId = 1, CustomerId = 1, TotalPrice = 200, Status = BookingStatus.Pending };
            _repositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);

            // Act
            var result = await _controller.GetBooking(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedBooking = Assert.IsType<Booking>(okResult.Value);
            Assert.Equal(1, returnedBooking.Id);
        }

        [Fact]
        public async Task GetBooking_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync((Booking)null);

            // Act
            var result = await _controller.GetBooking(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Booking not found.", notFoundResult.Value.GetType().GetProperty("Error")?.GetValue(notFoundResult.Value));
        }
    }
}