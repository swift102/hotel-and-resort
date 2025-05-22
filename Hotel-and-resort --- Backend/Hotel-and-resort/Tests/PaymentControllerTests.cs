using hotel_and_resort.Controllers;
using hotel_and_resort.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Xunit;
using Hotel_and_resort.Services;
using Hotel_and_resort.Controllers;
using Hotel_and_resort.ViewModels;

namespace HotelAndResort.Tests.Controllers
{
    public class PaymentControllerTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly PaymentController _controller;
        private readonly NullLogger<PaymentController> _logger;

        public PaymentControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // Setup mocks
            _repositoryMock = new Mock<IRepository>();
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["PayFast:MerchantId"]).Returns("10038419");
            _configurationMock.Setup(c => c["PayFast:MerchantKey"]).Returns("o6q83fm4xl05l");
            _configurationMock.Setup(c => c["PayFast:Passphrase"]).Returns("HOTELRESORTPAYFAST");
            _configurationMock.Setup(c => c["PayFast:ReturnUrl"]).Returns("https://yourapp.com/payment-success");
            _configurationMock.Setup(c => c["PayFast:CancelUrl"]).Returns("https://yourapp.com/payment-cancel");
            _configurationMock.Setup(c => c["PayFast:NotifyUrl"]).Returns("https://yourapi.com/api/payment/notify");

            // Setup logger
            _logger = new NullLogger<PaymentController>();

            // Initialize controller
            _controller = new PaymentController(_context, _repositoryMock.Object, _logger, _configurationMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task InitiatePayFastPayment_ValidBooking_ReturnsRedirectUrl()
        {
            // Arrange
            _context.Bookings.Add(new Booking
            {
                Id = 1,
                RoomId = 1,
                CustomerId = 1,
                TotalPrice = 500,
                Status = BookingStatus.Pending,
                Customer = new Customer { Id = 1, Email = "test@example.com" },
                Room = new Room { Id = 1 }
            });
            _context.SaveChanges();

            // Act
            var result = await _controller.InitiatePayFastPayment(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value.GetType().GetProperty("RedirectUrl")?.GetValue(okResult.Value)?.ToString();
            Assert.NotNull(response);
            Assert.Contains("https://sandbox.payfast.co.za/eng/process", response);
            Assert.Contains("merchant_id=10038419", response);
            Assert.Contains("amount=500.00", response);
            Assert.Contains("item_name=Booking+%231", response);
        }

        [Fact]
        public async Task InitiatePayFastPayment_NonExistingBooking_ReturnsNotFound()
        {
            // Arrange
            // No booking added to context

            // Act
            var result = await _controller.InitiatePayFastPayment(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Booking not found.", notFoundResult.Value.GetType().GetProperty("Error")?.GetValue(notFoundResult.Value));
        }

        [Fact]
        public async Task InitiatePayFastPayment_NonPendingBooking_ReturnsBadRequest()
        {
            // Arrange
            _context.Bookings.Add(new Booking
            {
                Id = 1,
                RoomId = 1,
                CustomerId = 1,
                TotalPrice = 500,
                Status = BookingStatus.Confirmed,
                Customer = new Customer { Id = 1, Email = "test@example.com" },
                Room = new Room { Id = 1 }
            });
            _context.SaveChanges();

            // Act
            var result = await _controller.InitiatePayFastPayment(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Booking is not in a payable state.", badRequestResult.Value.GetType().GetProperty("Error")?.GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task PayFastNotify_ValidNotification_ProcessesPayment()
        {
            // Arrange
            _context.Bookings.Add(new Booking
            {
                Id = 1,
                RoomId = 1,
                Status = BookingStatus.Pending
            });
            _context.SaveChanges();

            var notificationData = new Dictionary<string, string>
            {
                { "m_payment_id", "1" },
                { "payment_status", "COMPLETE" },
                { "amount_gross", "500.00" }
            };
            var signature = GenerateSignature(notificationData, "HOTELRESORTPAYFAST");
            notificationData["signature"] = signature;

            var queryString = string.Join("&", notificationData.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(queryString));
            _controller.HttpContext.Request.Body = stream;
            _controller.HttpContext.Request.ContentLength = stream.Length;

            _repositoryMock.Setup(r => r.AddPayment(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
            _repositoryMock.Setup(r => r.UpdateRoomAvailability(1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.PayFastNotify();

            // Assert
            Assert.IsType<OkResult>(result);
            var payment = _context.Payments.FirstOrDefault();
            Assert.NotNull(payment);
            Assert.Equal(1, payment.BookingId);
            Assert.Equal(50000, payment.Amount); // 500.00 * 100
            Assert.Equal(PaymentStatus.Completed, payment.Status);
            Assert.Equal(BookingStatus.Confirmed, _context.Bookings.First().Status);
        }

        [Fact]
        public async Task PayFastNotify_InvalidSignature_ReturnsBadRequest()
        {
            // Arrange
            var notificationData = new Dictionary<string, string>
            {
                { "m_payment_id", "1" },
                { "payment_status", "COMPLETE" },
                { "amount_gross", "500.00" },
                { "signature", "invalid_signature" }
            };
            var queryString = string.Join("&", notificationData.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(queryString));
            _controller.HttpContext.Request.Body = stream;
            _controller.HttpContext.Request.ContentLength = stream.Length;

            // Act
            var result = await _controller.PayFastNotify();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid signature.", badRequestResult.Value.GetType().GetProperty("Error")?.GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task PayFastNotify_NonExistingBooking_ReturnsNotFound()
        {
            // Arrange
            var notificationData = new Dictionary<string, string>
            {
                { "m_payment_id", "1" },
                { "payment_status", "COMPLETE" },
                { "amount_gross", "500.00" }
            };
            var signature = GenerateSignature(notificationData, "HOTELRESORTPAYFAST");
            notificationData["signature"] = signature;

            var queryString = string.Join("&", notificationData.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(queryString));
            _controller.HttpContext.Request.Body = stream;
            _controller.HttpContext.Request.ContentLength = stream.Length;

            // Act
            var result = await _controller.PayFastNotify();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Booking not found.", notFoundResult.Value.GetType().GetProperty("Error")?.GetValue(notFoundResult.Value));
        }

        private string GenerateSignature(Dictionary<string, string> data, string passphrase)
        {
            var sorted = data.OrderBy(x => x.Key).ToList();
            var sb = new StringBuilder();

            foreach (var kvp in sorted)
            {
                sb.Append($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}&");
            }

            if (!string.IsNullOrEmpty(passphrase))
                sb.Append("passphrase=" + HttpUtility.UrlEncode(passphrase));
            else
                sb.Length--;

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}