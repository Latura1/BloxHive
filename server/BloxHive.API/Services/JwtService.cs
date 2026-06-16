using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BloxHive.API.Services;

public class JwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config) => _config = config;

    public string GenerateUserToken(int userId, string username, DateTime? expiresAt)
    {
        var expireDays = _config.GetValue<int>("Jwt:UserExpireDays");
        return GenerateToken(userId, username, "user", DateTime.UtcNow.AddDays(expireDays));
    }

    public string GenerateAdminToken()
    {
        return GenerateToken(0, "admin", "admin", DateTime.UtcNow.AddHours(24));
    }

    private string GenerateToken(int userId, string username, string role, DateTime expires)
    {
        var secret = _config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Issuer"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
