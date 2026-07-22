using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Atlas.Modules.Auth.Application.Abstractions;
using Atlas.Modules.Auth.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Modules.Auth.Infrastructure.Security;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        // Claim = token icine gomulen "iddia" (bu kullanici su id'ye, su email'e sahip).
        // ICurrentUserAccessor'in gercek implementasyonu bunlari okuyacak.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            // ASP.NET Core'un policy-based authorization mekanizması (.RequireRole(...))
            // token'daki bu claim'i otomatik okuyor - başka hiçbir yerde elle
            // eşleştirme yapmamıza gerek yok.
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 8 saatten 15 dakikaya düşürüldü - access token artık kısa ömürlü,
        // çalınsa bile pencere dar. Uzun süre giriş kalabilmeyi artık refresh
        // token (RefreshToken.cs, 7 günlük ömür) sağlıyor.
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshTokenValue()
    {
        // JWT değil, kimlik iddiası taşımayan rastgele bir string - sadece
        // "bu değeri bilen, DB'deki şu kullanıcıya ait aktif bir refresh hakkına
        // sahiptir" anlamına geliyor. 64 byte -> tahmin edilemeyecek kadar geniş.
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}