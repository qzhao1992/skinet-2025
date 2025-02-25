using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Entities.Identity;
using Core.Interfaces;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly TokenOptions _tokenOptions;
    private readonly AppIdentityDbContext _context;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IOptions<TokenOptions> tokenOptions, AppIdentityDbContext context)
    {
        _tokenOptions = tokenOptions.Value;
        _context = context;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenOptions.Key));
    }

    public async Task<string> CreateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.DisplayName)
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_tokenOptions.ExpiryInMinutes),
            SigningCredentials = creds,
            Issuer = _tokenOptions.Issuer
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<string> CreateRefreshToken(AppUser user)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshToken = Convert.ToBase64String(randomNumber);

        var tokenEntity = new RefreshToken
        {
            AppUserId = user.Id,
            Token = refreshToken,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(_tokenOptions.RefreshTokenExpiryInDays)
        };

        _context.RefreshTokens.Add(tokenEntity);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<bool> RevokeRefreshToken(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null) return false;

        refreshToken.Revoked = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshToken(string expiredToken, string refreshToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateIssuer = true,
            ValidIssuer = _tokenOptions.Issuer,
            ValidateAudience = false,
            ValidateLifetime = false
        };

        var tokenInVerification = tokenHandler.ValidateToken(expiredToken, tokenValidationParameters, out var validatedToken);
        var jwtToken = (JwtSecurityToken)validatedToken;

        if (jwtToken == null)
            throw new SecurityTokenException("Invalid access token");

        var storedRefreshToken = await _context.RefreshTokens
            .Include(rt => rt.AppUser)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedRefreshToken == null)
            throw new SecurityTokenException("Invalid refresh token");

        if (!storedRefreshToken.IsActive)
            throw new SecurityTokenException("Refresh token expired or revoked");

        var newAccessToken = await CreateToken(storedRefreshToken.AppUser);
        var newRefreshToken = await CreateRefreshToken(storedRefreshToken.AppUser);

        await RevokeRefreshToken(refreshToken);

        return (newAccessToken, newRefreshToken);
    }
}