using Microsoft.AspNetCore.Mvc;
using SecureChat.DAL.Repositories;
using System.Threading.Tasks;

namespace SecureChat.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // search using username
        [HttpGet("search/{username}")]
        public async Task<IActionResult> SearchUser(string username)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);

            if (user == null)
                return NotFound(new { Message = "User does not exist" });

            // return only general data not including password and critical data
            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                PublicKey = user.PublicKey
            });
        }
    }
}