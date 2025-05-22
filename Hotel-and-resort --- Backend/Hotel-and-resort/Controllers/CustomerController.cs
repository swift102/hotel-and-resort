using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace hotel_and_resort.Controllers
{
    [Route("api/customers")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerService _customerService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(CustomerService customerService, ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers(int page = 1, int pageSize = 10)
        {
            try
            {
                var customers = await _customerService.GetCustomersAsync(page, pageSize);
                return Ok(customers);
            }
            catch (CustomerServiceException ex)
            {
                _logger.LogError(ex, "Error fetching customers, page {Page}, pageSize {PageSize}", page, pageSize);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found: {CustomerId}", id);
                    return NotFound(new { Error = $"Customer with ID {id} not found." });
                }

                // Restrict to admin or the customer themselves
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!User.IsInRole("Admin") && customer.Id.ToString() != userId)
                {
                    _logger.LogWarning("Unauthorized access to customer {CustomerId} by user {UserId}", id, userId);
                    return Forbid();
                }

                return Ok(customer);
            }
            catch (CustomerServiceException ex)
            {
                _logger.LogError(ex, "Error fetching customer with ID {CustomerId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Customer>> AddCustomer([FromBody] CustomerCreateDTO customerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid customer data provided.");
                return BadRequest(ModelState);
            }

            try
            {
                var customer = new Customer
                {
                    FirstName = customerDto.FirstName,
                    LastName = customerDto.LastName,
                    Email = customerDto.Email,
                    Phone = customerDto.Phone
                };

                var addedCustomer = await _customerService.AddCustomerAsync(customer);
                _logger.LogInformation("Customer created: {CustomerId}", addedCustomer.Id);
                return CreatedAtAction(nameof(GetCustomer), new { id = addedCustomer.Id }, addedCustomer);
            }
            catch (DuplicateCustomerException ex)
            {
                _logger.LogWarning("Duplicate customer error: {Message}", ex.Message);
                return Conflict(new { Error = ex.Message });
            }
            catch (CustomerValidationException ex)
            {
                _logger.LogWarning("Validation error: {Message}", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
            catch (CustomerServiceException ex)
            {
                _logger.LogError(ex, "Error adding customer with email {Email}", customerDto.Email);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDTO customerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid customer data provided for update.");
                return BadRequest(ModelState);
            }

            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found: {CustomerId}", id);
                    return NotFound(new { Error = $"Customer with ID {id} not found." });
                }

                customer.FirstName = customerDto.FirstName;
                customer.LastName = customerDto.LastName;
                customer.Email = customerDto.Email;
                customer.Phone = customerDto.Phone;

                await _customerService.UpdateCustomerAsync(customer);
                _logger.LogInformation("Customer updated: {CustomerId}", id);
                return NoContent();
            }
            catch (DuplicateCustomerException ex)
            {
                _logger.LogWarning("Duplicate customer error: {Message}", ex.Message);
                return Conflict(new { Error = ex.Message });
            }
            catch (CustomerValidationException ex)
            {
                _logger.LogWarning("Validation error: {Message}", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
            catch (CustomerServiceException ex)
            {
                _logger.LogError(ex, "Error updating customer with ID {CustomerId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
                _logger.LogInformation("Customer deleted: {CustomerId}", id);
                return NoContent();
            }
            catch (CustomerNotFoundException ex)
            {
                _logger.LogWarning("Customer not found: {Message}", ex.Message);
                return NotFound(new { Error = ex.Message });
            }
            catch (CustomerServiceException ex)
            {
                _logger.LogError(ex, "Error deleting customer with ID {CustomerId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }

    public class CustomerCreateDTO
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        [Required]
        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; }
    }

    public class CustomerUpdateDTO
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        [Required]
        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; }
    }

}
