using hotel_and_resort.Controllers;
using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_and_resort.ViewModels;

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


        [HttpPut("EditUserProfileByID{UserProfileID}")]
        public async Task<IActionResult> EditUserProfile(int UserProfileID, [FromBody] UserProfileDto userProfileDto)
        {
            var user = await _context.UserProfiles.FindAsync(UserProfileID);
            if (user == null)
            {
                return NotFound();
            }

            user.UserProfileID = userProfileDto.UserProfileID;
            user.ProfileDescription = userProfileDto.ProfileDescription;
            _context.UserProfiles.Update(user);

            var result = await _userManager.FindByIdAsync(user.UserProfileID.ToString());
            if (result != null)
            {
                user.UserProfileID = userProfileDto.UserProfileID;
                user.ProfileDescription = userProfileDto.ProfileDescription;
                await _userManager.UpdateAsync(result);
            }

            var saveResult = await _context.SaveChangesAsync();
            if (saveResult == 0)
            {
                return BadRequest("Failed to update the agent.");
            }

            return Ok(user);
        }

        [HttpDelete("DeleteUserProfileByID{UserProfileID}")]
        public async Task DeleteUserProfile(int UserProfileID)
        {
            var userProfile = await _context.UserProfiles.FindAsync(UserProfileID);
            if (userProfile == null) return;

            _context.UserProfiles.Remove(userProfile);
            await _context.SaveChangesAsync();
        }
    }
}
