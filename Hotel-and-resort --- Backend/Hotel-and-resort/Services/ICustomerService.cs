using hotel_and_resort.Models;
using System.Threading.Tasks;


namespace Hotel_and_resort.Services
{
    public interface ICustomerService
    {
        Task<Customer> GetCustomerByIdAsync(int id);
        Task<Customer> GetCustomerByEmailAsync(string email);
        Task<IEnumerable<Customer>> GetCustomersAsync(int page, int pageSize);
        Task<Customer> AddCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(int id);
        Task<Customer> GetCustomerByUserProfileIdAsync(int userProfileId);
        Task<Customer> GetCustomerByUserIdAsync(string userId);
    }
}
