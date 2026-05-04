using System;
using System.Threading.Tasks;
using SecureChat.DAL.Entities;

namespace SecureChat.DAL.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(Guid id);
        Task<User> GetUserByUsernameAsync(string username);
        Task AddUserAsync(User user);
        Task<bool> SaveChangesAsync();
    }
}