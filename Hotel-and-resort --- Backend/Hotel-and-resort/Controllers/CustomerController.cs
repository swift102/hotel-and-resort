using hotel_and_resort.DTOs;
using hotel_and_resort.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hotel_and_resort.Services;

namespace hotel_and_resort.Controllers
{
    [Route("api/[controller]")]
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
        public async Task<ActionResult<IEnumerable<CustomerReadDTO>>> GetCustomers(int page = 1, int pageSize = 10)
        {
            var customers = await _customerService.GetCustomersAsync(page, pageSize);
            var customerDtos = customers.Select(c => new CustomerReadDTO
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone
            });
            return Ok(customerDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerReadDTO>> GetCustomer(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            var customerDto = new CustomerReadDTO
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone
            };
            return Ok(customerDto);
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer([FromBody] CustomerCreateDTO customerDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var customer = new Customer
                {
                    FirstName = customerDto.FirstName,
                    LastName = customerDto.LastName,
                    Email = customerDto.Email,
                    Phone = customerDto.Phone,
                    Title = customerDto.Title
                };

                var addedCustomer = await _customerService.AddCustomerAsync(customer);
                return CreatedAtAction(nameof(GetCustomer), new { id = addedCustomer.Id }, addedCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDTO customerDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for ID: {Id}", id);
                    return NotFound();
                }

                customer.FirstName = customerDto.FirstName;
                customer.LastName = customerDto.LastName;
                customer.Email = customerDto.Email;
                customer.Phone = customerDto.Phone;
                customer.Title = customerDto.Title;

                await _customerService.UpdateCustomerAsync(customer);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null) return NotFound();

                await _customerService.DeleteCustomerAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer");
                return StatusCode(500, "Internal server error");
            }
        }
    }

}
