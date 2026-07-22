using System.Security.Claims;
using Atlas.Shared.Contracts;
using Microsoft.AspNetCore.Http;

namespace Atlas.Modules.Auth.Infrastructure.CurrentUser;

/// <summary>
/// FakeCurrentUserAccessor'in yerini aliyor. Artik "ilk kullaniciyi admin say"
/// yok - JWT middleware'in HttpContext.User'a yazdigi claim'leri okuyoruz.
/// Token yoksa ya da gecersizse IsAuthenticated false doner.
/// </summary>
public class HttpCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Department => User?.FindFirst("department")?.Value;
}