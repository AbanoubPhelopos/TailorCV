using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TailorCV.Identity.Domain;
using TailorCV.Shared.Interfaces;

namespace TailorCV.Identity.Infrastructure;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 10080)]
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

public interface IJwtService
{
    string GenerateAccessToken(User user);
}

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtService(IOptions<JwtSettings> jwtSettings, IDateTimeProvider dateTimeProvider)
    {
        _jwtSettings = jwtSettings.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    public string GenerateAccessToken(User user)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        ];

        JwtSecurityToken token = new(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: _dateTimeProvider.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
