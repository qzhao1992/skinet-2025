using API.DTOs;
using Core.Entities.Identity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
        {
            return BadRequest("Email address is in use");
        }

        var user = new AppUser
        {
            DisplayName = registerDto.DisplayName,
            Email = registerDto.Email,
            UserName = registerDto.Email
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var accessToken = await _tokenService.CreateToken(user);
        string refreshToken = null;

        if (registerDto.RememberMe)
        {
            refreshToken = await _tokenService.CreateRefreshToken(user);
        }

        return new UserDto
        {
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = accessToken,
            RefreshToken = refreshToken
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);

        if (user == null)
        {
            return Unauthorized("Invalid email");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

        if (!result.Succeeded)
        {
            return Unauthorized("Invalid password");
        }

        var accessToken = await _tokenService.CreateToken(user);
        string refreshToken = null;

        if (loginDto.RememberMe)
        {
            refreshToken = await _tokenService.CreateRefreshToken(user);
        }

        return new UserDto
        {
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = accessToken,
            RefreshToken = refreshToken
        };
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var (accessToken, refreshToken) = await _tokenService.RefreshToken(
                refreshTokenDto.AccessToken,
                refreshTokenDto.RefreshToken);

            return new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest("Token is required");
        }

        var result = await _tokenService.RevokeRefreshToken(refreshToken);

        if (!result)
        {
            return NotFound("Token not found");
        }

        return Ok();
    }
}