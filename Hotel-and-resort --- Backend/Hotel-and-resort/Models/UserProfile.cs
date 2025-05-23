using hotel_and_resort.Models;
using System.ComponentModel.DataAnnotations;
namespace Hotel_and_resort.Models
{
    public class UserProfile
    {
        [Key]
        public int UserProfileID { get; set; }
        [MaxLength(500)]
        public string? ProfileDescription { get; set; }
        public User? User { get; set; }
    }
}
