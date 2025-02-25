using Core.Entities.Identity;

namespace Core.Interfaces;

public interface ITokenService
{
    Task<string> CreateToken(AppUser user);
    Task<string> CreateRefreshToken(AppUser user);
    Task<bool> RevokeRefreshToken(string token);
    Task<(string AccessToken, string RefreshToken)> RefreshToken(string expiredToken, string refreshToken);
}