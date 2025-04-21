using FluentValidation;
using hotel_and_resort.Models;

namespace Hotel_and_resort.ErrorHandling
{

    public class Customer
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; }
        public string? Title { get; set; }


    }
    public class CustomerValidators : AbstractValidator<Customer>
    {
        public CustomerValidators()
        {
            RuleFor(c => c.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(c => c.LastName).NotEmpty().MaximumLength(50);
            RuleFor(c => c.Email).NotEmpty().EmailAddress();
            RuleFor(c => c.Phone).NotEmpty().Matches(@"^\+\d{10,}$");
        }
    }
}
