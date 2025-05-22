//using hotel_and_resort.Controllers;
//using hotel_and_resort.DTOs;
//using hotel_and_resort.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Logging.Abstractions;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Xunit;



//namespace HotelAndResort.Tests.Controllers
//{
//    public class BookingControllerTests
//    {
//        private readonly AppDbContext _context;
//        private readonly Mock<IRepository> _repositoryMock;
//        private readonly BookingController _controller;
//        private readonly NullLogger<BookingController> _logger;
//        private readonly Mock<IEmailSender> _emailSenderMock; 
//        private readonly Mock<IMemoryCache> _memoryCacheMock; 

//        public BookingControllerTests()
//        {
//            // Setup in-memory database
//            var options = new DbContextOptionsBuilder<AppDbContext>()
//                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB per test
//                .Options;
//            _context = new AppDbContext(options);

//            // Setup repository mock
//            _repositoryMock = new Mock<IRepository>();

//            // Setup logger
//            _logger = new NullLogger<BookingController>();

//            // Setup email sender mock
//            _emailSenderMock = new Mock<IEmailSender>();

//            // Setup memory cache mock
//            _memoryCacheMock = new Mock<IMemoryCache>();

//            // Initialize controller with all required dependencies
//            _controller = new BookingController(_context, _repositoryMock.Object, _logger, _emailSenderMock.Object, _memoryCacheMock.Object);
//        }
//    }
//}