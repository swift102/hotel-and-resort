using hotel_and_resort.Models;
namespace Hotel_and_resort.Models
{
    public class UserProfile
    {
        public int? UserProfileID { get; set; }
        public string? ProfileDescription { get; set; }
        public ICollection<User> Users { get; set; }
    }
}
