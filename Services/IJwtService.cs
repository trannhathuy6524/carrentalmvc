using carrentalmvc.Models;

namespace carrentalmvc.Services
{
    public interface IJwtService
    {
        Task<string> GenerateJwtToken(ApplicationUser user);
        Task<string> GenerateRefreshToken();
        Task<bool> ValidateRefreshToken(string userId, string refreshToken);
        Task SaveRefreshToken(string userId, string refreshToken);
        Task RevokeRefreshToken(string userId, string refreshToken);
    }
}