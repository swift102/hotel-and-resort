using Ganss.Xss;
using hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;

namespace hotel_and_resort.Services
{
    public class CustomerService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomerService> _logger;
        private readonly IHtmlSanitizer _sanitizer;

        public CustomerService(AppDbContext context, ILogger<CustomerService> logger)
        {
            _context = context;
            _logger = logger;
            _sanitizer = new HtmlSanitizer();
        }

        public async Task<IEnumerable<Customer>> GetCustomersAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                return await _context.Customers
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers, page {Page}, pageSize {PageSize}", page, pageSize);
                throw new CustomerServiceException("Failed to retrieve customers.", ex);
            }
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            try
            {
                return await _context.Customers
                    .Include(c => c.Bookings)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer with ID {CustomerId}", id);
                throw new CustomerServiceException($"Failed to retrieve customer with ID {id}.", ex);
            }
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Invalid email provided for lookup.");
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            try
            {
                return await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer with email {Email}", email);
                throw new CustomerServiceException($"Failed to retrieve customer with email {email}.", ex);
            }
        }

        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            if (customer == null)
            {
                _logger.LogWarning("Attempted to add null customer.");
                throw new ArgumentNullException(nameof(customer));
            }

            try
            {
                // Sanitize inputs
                customer.FirstName = _sanitizer.Sanitize(customer.FirstName);
                customer.LastName = _sanitizer.Sanitize(customer.LastName);
                customer.Email = _sanitizer.Sanitize(customer.Email);
                customer.Phone = _sanitizer.Sanitize(customer.Phone);

                // Validate inputs
                if (string.IsNullOrWhiteSpace(customer.FirstName) || string.IsNullOrWhiteSpace(customer.LastName) ||
                    string.IsNullOrWhiteSpace(customer.Email) || string.IsNullOrWhiteSpace(customer.Phone))
                {
                    _logger.LogWarning("Invalid customer data provided.");
                    throw new CustomerValidationException("All customer fields are required.");
                }

                // Check for duplicates
                if (await _context.Customers.AnyAsync(c => c.Email == customer.Email))
                {
                    _logger.LogWarning("Duplicate email detected: {Email}", customer.Email);
                    throw new DuplicateCustomerException($"Email {customer.Email} already exists.");
                }
                if (await _context.Customers.AnyAsync(c => c.Phone == customer.Phone))
                {
                    _logger.LogWarning("Duplicate phone detected: {Phone}", customer.Phone);
                    throw new DuplicateCustomerException($"Phone {customer.Phone} already exists.");
                }

                var result = await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer added successfully: {CustomerId}", result.Entity.Id);
                return result.Entity;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error adding customer with email {Email}", customer.Email);
                throw new CustomerServiceException("Failed to add customer due to database error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer with email {Email}", customer.Email);
                throw new CustomerServiceException("Failed to add customer.", ex);
            }
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            if (customer == null)
            {
                _logger.LogWarning("Attempted to update null customer.");
                throw new ArgumentNullException(nameof(customer));
            }

            try
            {
                // Sanitize inputs
                customer.FirstName = _sanitizer.Sanitize(customer.FirstName);
                customer.LastName = _sanitizer.Sanitize(customer.LastName);
                customer.Email = _sanitizer.Sanitize(customer.Email);
                customer.Phone = _sanitizer.Sanitize(customer.Phone);

                // Validate inputs
                if (string.IsNullOrWhiteSpace(customer.FirstName) || string.IsNullOrWhiteSpace(customer.LastName) ||
                    string.IsNullOrWhiteSpace(customer.Email) || string.IsNullOrWhiteSpace(customer.Phone))
                {
                    _logger.LogWarning("Invalid customer data provided for update.");
                    throw new CustomerValidationException("All customer fields are required.");
                }

                // Check for duplicates
                if (await _context.Customers.AnyAsync(c => c.Email == customer.Email && c.Id != customer.Id))
                {
                    _logger.LogWarning("Duplicate email detected: {Email}", customer.Email);
                    throw new DuplicateCustomerException($"Email {customer.Email} already exists.");
                }
                if (await _context.Customers.AnyAsync(c => c.Phone == customer.Phone && c.Id != customer.Id))
                {
                    _logger.LogWarning("Duplicate phone detected: {Phone}", customer.Phone);
                    throw new DuplicateCustomerException($"Phone {customer.Phone} already exists.");
                }

                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer updated successfully: {CustomerId}", customer.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating customer with ID {CustomerId}", customer.Id);
                throw new CustomerServiceException($"Failed to update customer with ID {customer.Id} due to database error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer with ID {CustomerId}", customer.Id);
                throw new CustomerServiceException($"Failed to update customer with ID {customer.Id}.", ex);
            }
        }

        public async Task DeleteCustomerAsync(int id)
        {
            try
            {
                var customer = await GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for deletion: {CustomerId}", id);
                    throw new CustomerNotFoundException($"Customer with ID {id} not found.");
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer deleted successfully: {CustomerId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer with ID {CustomerId}", id);
                throw new CustomerServiceException($"Failed to delete customer with ID {id}.", ex);
            }
        }
    }

    public class CustomerServiceException : Exception
    {
        public CustomerServiceException(string message) : base(message) { }
        public CustomerServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class CustomerValidationException : Exception
    {
        public CustomerValidationException(string message) : base(message) { }
    }

    public class DuplicateCustomerException : Exception
    {
        public DuplicateCustomerException(string message) : base(message) { }
    }

    public class CustomerNotFoundException : Exception
    {
        public CustomerNotFoundException(string message) : base(message) { }
    }
}