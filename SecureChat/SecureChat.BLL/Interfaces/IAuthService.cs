using System.Threading.Tasks;
using SecureChat.BLL.DTOs.Auth;

namespace SecureChat.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDto> RegisterAsync(RegisterDto dto);
        Task<AuthResultDto> LoginAsync(string username, string password);
    }
}