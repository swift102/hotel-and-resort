using System.ComponentModel.DataAnnotations;

namespace Hotel_and_resort.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}
