using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;

namespace LanguageLearningApp.API.Application.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto> LoginAsync(LoginDto loginDto);
    }
}