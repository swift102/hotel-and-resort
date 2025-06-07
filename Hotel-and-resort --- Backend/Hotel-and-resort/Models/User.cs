using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace Hotel_and_resort.Models

{
        public class User : IdentityUser
        {
            public string? Name { get; set; }
            public string? Surname { get; set; }
            public string? ContactNumber { get; set; }
            public int UserProfileID { get; set; }

            public UserProfile? UserProfile { get; set; }

            // Additional fields for verification
            public string? VerificationCode { get; set; }
            public DateTime? VerificationCodeExpiration { get; set; }

     
        }

}

