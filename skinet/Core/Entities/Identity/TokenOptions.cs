namespace Core.Entities.Identity;

public class TokenOptions
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public int ExpiryInMinutes { get; set; }
    public int RefreshTokenExpiryInDays { get; set; }
}