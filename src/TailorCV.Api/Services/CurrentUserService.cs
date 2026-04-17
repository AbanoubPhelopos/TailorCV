using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TailorCV.Shared.Interfaces;

namespace TailorCV.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            Claim? claim = _httpContextAccessor.HttpContext?.User.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out Guid userId)
                ? userId
                : Guid.Empty;
        }
    }

    public string Email =>
        _httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value ?? string.Empty;

    public string Role =>
        _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value ?? string.Empty;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
