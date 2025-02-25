namespace API.DTOs;

public class UserDto
{
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}

public class TokenDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}

public class RefreshTokenDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}