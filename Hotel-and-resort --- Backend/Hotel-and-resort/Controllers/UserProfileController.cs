using hotel_and_resort.Controllers;
using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_and_resort.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace Hotel_and_resort.Controllers
{
    public class UserProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRepository _repository;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(AppDbContext context, UserManager<User> userManager, IRepository repository, ILogger<UserProfileController> logger)
        {
            _context = context;
            _repository = repository;
            _logger = logger;
        }


        [HttpGet("user-profiles")]
        public async Task<ActionResult<IEnumerable<UserProfile>>> GetUserProfiles()
        {
            return await _context.UserProfiles.ToListAsync();
        }

        [HttpGet]
        [Route("GetUserProfileByID/{UserProfileId}")]
        public async Task<IActionResult> GetUserProfileByID(int UserProfileId)
        {
            return Ok(await _repository.GetUserProfileByID(UserProfileId));
        }

        [HttpPost("addUserProfile")]
        public async Task<UserProfileDto> AddUserProfile(UserProfileDto userProfileDto)
        {
            var userProfile = new UserProfile
            {
                ProfileDescription = userProfileDto.ProfileDescription
            };

            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync();

            userProfileDto.UserProfileID = userProfile.UserProfileID;

            return userProfileDto;
        }


        [HttpPut("EditUserProfileByID/{UserProfileID}")]
        public async Task<IActionResult> EditUserProfile(int UserProfileID, [FromBody] UserProfileDto userProfileDto)
        {
            var userProfile = await _context.UserProfiles.FindAsync(UserProfileID);
            if (userProfile == null) return NotFound();

            userProfile.ProfileDescription = userProfileDto.ProfileDescription;
            _context.UserProfiles.Update(userProfile);

            var saveResult = await _context.SaveChangesAsync();
            if (saveResult == 0) return BadRequest("Failed to update the profile.");

            return Ok(userProfile);
        }

        [HttpDelete("DeleteUserProfileByID{UserProfileID}")]
        public async Task DeleteUserProfile(int UserProfileID)
        {
            var userProfile = await _context.UserProfiles.FindAsync(UserProfileID);
            if (userProfile == null) return;

            _context.UserProfiles.Remove(userProfile);
            await _context.SaveChangesAsync();
        }

        [HttpGet("admin/users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsersWithRoles()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.Name,
                    user.Surname,
                    user.ContactNumber,
                    user.UserProfileID,
                    Roles = roles
                });
            }
            return Ok(userDtos);
        }

        [HttpPut("admin/users/{id}/roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("User not found.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRolesAsync(user, roles);

            return Ok(new { Message = "User roles updated successfully." });
        }
    }
}
