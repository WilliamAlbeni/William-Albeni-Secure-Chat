using System;
using System.Threading.Tasks;
using SecureChat.BLL.DTOs.Auth;
using SecureChat.BLL.Interfaces;
using SecureChat.BLL.Settings;
using SecureChat.DAL.Entities;
using SecureChat.DAL.Repositories;
using Microsoft.Extensions.Options;

namespace SecureChat.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly string _pepper;

        
        public AuthService(IUserRepository userRepository, IOptions<SecuritySettings> securitySettings)
        {
            _userRepository = userRepository;
            _pepper = securitySettings.Value.PasswordPepper;
        }

        public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
        {
            // 1. Checking User duplicate
            var existingUser = await _userRepository.GetUserByUsernameAsync(dto.Username);
            if (existingUser != null)
                throw new Exception("Username already exists, please choose another username.");

            // 2. Concatenate password with pepper
            string pepperedPassword = dto.Password + _pepper;

            // 3. Hashing
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(pepperedPassword, workFactor: 12); // workFactor = Iterations

            // 4. Creating object to save it
            var newUser = new User
            {
                Username = dto.Username,
                PasswordHash = hashedPassword,
                PublicKey = dto.PublicKey
            };

            await _userRepository.AddUserAsync(newUser);
            await _userRepository.SaveChangesAsync();

            // 5. returning result as DTO
            return new AuthResultDto
            {
                UserId = newUser.Id,
                Username = newUser.Username,
                PublicKey = newUser.PublicKey
            };
        }

        public async Task<AuthResultDto> LoginAsync(string username, string password)
        {
            // 1. Search for user
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null)
                return null;

            // 2. concatenate the inserted password with the same pepper
            string pepperedPassword = password + _pepper;

            // 3. Validating
            bool isValid = BCrypt.Net.BCrypt.Verify(pepperedPassword, user.PasswordHash);
            if (!isValid)
                return null;

            return new AuthResultDto
            {
                UserId = user.Id,
                Username = user.Username,
                PublicKey = user.PublicKey
            };
        }
    }
}