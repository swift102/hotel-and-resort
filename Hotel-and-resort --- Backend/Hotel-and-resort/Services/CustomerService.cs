using hotel_and_resort.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hotel_and_resort.Services;
using AngleSharp.Text;


namespace Hotel_and_resort.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(AppDbContext context, ILogger<CustomerService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for ID {CustomerId}", id);
                    throw new CustomerNotFoundException($"Customer with ID {id} not found.");
                }
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer for ID {CustomerId}", id);
                throw new CustomerServiceException($"Failed to fetch customer for ID {id}.", ex);
            }
        }

        public async Task<Customer> GetCustomerByUserIdAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Invalid userId provided for customer lookup.");
                    throw new CustomerValidationException("UserId is required.");
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for userId {UserId}", userId);
                    throw new CustomerNotFoundException($"Customer with UserId {userId} not found.");
                }

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer for userId {UserId}", userId);
                throw new CustomerServiceException($"Failed to fetch customer for userId {userId}.", ex);
            }
        }

        public async Task<Customer> GetCustomerByUserProfileIdAsync(int userProfileId)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(userProfileId);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for ID {UserProfileID}", userProfileId);
                    throw new CustomerNotFoundException($"Customer with UserProfileID {userProfileId} not found.");
                }

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer for UserProfileID {UserProfileID}", userProfileId);
                throw new CustomerServiceException($"Failed to fetch customer for UserProfileID {userProfileId}.", ex);
            }
        }

        public async Task<Customer> GetCustomerByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Invalid email provided for customer lookup.");
                    throw new CustomerValidationException("Email is required.");
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == email);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for email {Email}", email);
                    return null;
                }
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer for email {Email}", email);
                throw new CustomerServiceException($"Failed to fetch customer for email {email}.", ex);
            }
        }

        public async Task<IEnumerable<Customer>> GetCustomersAsync(int page, int pageSize)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                {
                    _logger.LogWarning("Invalid pagination parameters: page={Page}, pageSize={PageSize}", page, pageSize);
                    throw new CustomerValidationException("Page and page size must be positive.");
                }

                var customers = await _context.Customers
                    .OrderBy(c => c.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers, page {Page}, pageSize {PageSize}", page, pageSize);
                throw new CustomerServiceException($"Failed to fetch customers for page {page}.", ex);
            }
        }

        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    _logger.LogWarning("Attempted to add null customer.");
                    throw new ArgumentNullException(nameof(customer));
                }

                // Validate inputs
                if (string.IsNullOrWhiteSpace(customer.FirstName) ||
                    string.IsNullOrWhiteSpace(customer.LastName) ||
                    string.IsNullOrWhiteSpace(customer.Email) ||
                    string.IsNullOrWhiteSpace(customer.Phone))
                {
                    _logger.LogWarning("Invalid customer data: FirstName={FirstName}, LastName={LastName}, Email={Email}, Phone={Phone}",
                        customer.FirstName, customer.LastName, customer.Email, customer.Phone);
                    throw new CustomerValidationException("Customer first name, last name, email, and phone are required.");
                }

                if (!IsValidEmail(customer.Email))
                {
                    _logger.LogWarning("Invalid email address: {Email}", customer.Email);
                    throw new CustomerValidationException("Invalid email address.");
                }

                // Check for duplicate email
                var existing = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == customer.Email);
                if (existing != null)
                {
                    _logger.LogWarning("Customer already exists for email {Email}", customer.Email);
                    throw new DuplicateCustomerException($"Customer with email {customer.Email} already exists.");
                }

                // Check for duplicate UserId if provided
                if (!string.IsNullOrWhiteSpace(customer.UserId))
                {
                    var existingUser = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == customer.UserId);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("Customer already exists for UserId {UserId}", customer.UserId);
                        throw new DuplicateCustomerException($"Customer with UserId {customer.UserId} already exists.");
                    }
                }

                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer created: Id={CustomerId}, Email={Email}", customer.Id, customer.Email);
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer with email {Email}", customer.Email);
                throw new CustomerServiceException($"Failed to add customer with email {customer.Email}.", ex);
            }
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    _logger.LogWarning("Attempted to update null customer.");
                    throw new ArgumentNullException(nameof(customer));
                }

                // Validate inputs
                if (string.IsNullOrWhiteSpace(customer.FirstName) ||
                    string.IsNullOrWhiteSpace(customer.LastName) ||
                    string.IsNullOrWhiteSpace(customer.Email) ||
                    string.IsNullOrWhiteSpace(customer.Phone))
                {
                    _logger.LogWarning("Invalid customer data: FirstName={FirstName}, LastName={LastName}, Email={Email}, Phone={Phone}",
                        customer.FirstName, customer.LastName, customer.Email, customer.Phone);
                    throw new CustomerValidationException("Customer first name, last name, email, and phone are required.");
                }

                if (!IsValidEmail(customer.Email))
                {
                    _logger.LogWarning("Invalid email address: {Email}", customer.Email);
                    throw new CustomerValidationException("Invalid email address.");
                }

                var existing = await _context.Customers.FindAsync(customer.Id);
                if (existing == null)
                {
                    _logger.LogWarning("Customer not found for Id {CustomerId}", customer.Id);
                    throw new CustomerNotFoundException($"Customer with ID {customer.Id} not found.");
                }

                // Check for duplicate email (excluding current customer)
                var duplicate = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == customer.Email && c.Id != customer.Id);
                if (duplicate != null)
                {
                    _logger.LogWarning("Another customer already exists with email {Email}", customer.Email);
                    throw new DuplicateCustomerException($"Another customer with email {customer.Email} already exists.");
                }

                existing.FirstName = customer.FirstName;
                existing.LastName = customer.LastName;
                existing.Email = customer.Email;
                existing.Phone = customer.Phone;
                // Note: Do not update UserId to prevent reassociation

                _context.Customers.Update(existing);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer updated: Id={CustomerId}, Email={Email}", existing.Id, existing.Email);
                return existing;
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
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for Id {CustomerId}", id);
                    throw new CustomerNotFoundException($"Customer with ID {id} not found.");
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer deleted: Id={CustomerId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer with ID {CustomerId}", id);
                throw new CustomerServiceException($"Failed to delete customer with ID {id}.", ex);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
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
