using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloxHive.API.Data;
using BloxHive.API.Models;
using BloxHive.API.Services;

namespace BloxHive.API.Controllers;

[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Key) || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Alle Felder sind erforderlich." });

        if (req.Username.Length < 3 || req.Username.Length > 50)
            return BadRequest(new { error = "Username muss 3-50 Zeichen lang sein." });

        if (req.Password.Length < 6)
            return BadRequest(new { error = "Passwort muss mindestens 6 Zeichen lang sein." });

        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict(new { error = "Username bereits vergeben." });

        var key = await _db.LicenseKeys.FirstOrDefaultAsync(k => k.Key == req.Key && !k.IsUsed);
        if (key == null)
            return NotFound(new { error = "Ungültiger oder bereits verwendeter Key." });

        if (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "Dieser Key ist abgelaufen." });

        var user = new User
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            LicenseKeyId = key.Id,
            ExpiresAt = key.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        key.IsUsed = true;
        key.UsedByUserId = user.Id;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Account erstellt. Du kannst dich jetzt einloggen." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Username und Passwort erforderlich." });

        if (string.IsNullOrWhiteSpace(req.Hwid))
            return BadRequest(new { error = "HWID erforderlich." });

        var user = await _db.Users.Include(u => u.LicenseKey).FirstOrDefaultAsync(u => u.Username == req.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Falscher Username oder Passwort." });

        if (!user.IsActive)
            return Unauthorized(new { error = "Account wurde deaktiviert." });

        if (user.ExpiresAt.HasValue && user.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { error = "Account ist abgelaufen." });

        if (string.IsNullOrEmpty(user.HwidHash))
        {
            user.HwidHash = HashHwid(req.Hwid);
            user.HwidLockedAt = DateTime.UtcNow;
        }
        else if (user.HwidHash != HashHwid(req.Hwid))
        {
            return Unauthorized(new { error = "HWID gebunden an anderen Rechner. Kontaktiere den Admin." });
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.LoginSessionStart = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateUserToken(user.Id, user.Username, user.ExpiresAt);

        return Ok(new LoginResponse(token, new UserInfo(user.Id, user.Username, user.ExpiresAt, user.IsActive, user.ForceLogoutAt, user.CreatedAt, null)));
    }

    [HttpPost("verify"), Authorize]
    public async Task<IActionResult> Verify()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(userId);

        if (user == null || !user.IsActive)
            return Unauthorized(new { error = "Account nicht gefunden oder deaktiviert." });

        if (user.ExpiresAt.HasValue && user.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { error = "Account ist abgelaufen." });

        if (user.ForceLogoutAt.HasValue)
        {
            var tokenIat = User.FindFirst("iat")?.Value;
            if (tokenIat != null && long.TryParse(tokenIat, out var iatSecs))
            {
                var tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(iatSecs).UtcDateTime;
                if (user.ForceLogoutAt > tokenIssuedAt)
                    return Unauthorized(new { error = "Account wurde ausgeloggt." });
            }
        }

        user.LastVerifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new UserInfo(user.Id, user.Username, user.ExpiresAt, user.IsActive, user.ForceLogoutAt, user.CreatedAt, user.LastVerifiedAt));
    }

    private static string HashHwid(string hwid)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hwid));
        return Convert.ToHexString(bytes).ToLower();
    }
}
