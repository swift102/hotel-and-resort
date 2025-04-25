using Hotel_and_resort.Models;

namespace Hotel_and_resort.ViewModels
{
    public class UserDto
    {

        public string Name { get; set; }
        public string Surname { get; set; }
        public string ContactNumber { get; set; }
        public int UserProfileID { get; set; }
        public string UserProfileDescription { get; set; }
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeExpiration { get; set; }
        public string Id { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string ConcurrencyStamp { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
    }


    public class CreateUserDto
    {
        public string? Name { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Surname { get; set; }
        public string? ContactNumber { get; set; }
        public int UserProfileID { get; set; }
    }

    public class EditUserDto
    {
        public string UserName { get; set; }
        public int UserProfileID { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string ContactNumber { get; set; }
    }

    public class UserRolesDto
    {
        public string UserName { get; set; }
        public string RoleName { get; set; }
    }

    public class RegisterDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // Add this property
    }

}
