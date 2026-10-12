namespace SWP.Infrastructure.Options;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public string SecretKey { get; set; } = null!;

    public int ExpirationMinutes { get; set; } = 480;
}
