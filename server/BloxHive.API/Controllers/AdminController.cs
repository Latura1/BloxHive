using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloxHive.API.Data;
using BloxHive.API.Models;
using BloxHive.API.Services;

namespace BloxHive.API.Controllers;

[ApiController, Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly IConfiguration _config;

    public AdminController(AppDbContext db, JwtService jwt, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest req)
    {
        var adminPassword = _config["Admin:Password"];
        if (string.IsNullOrEmpty(adminPassword) || req.Password != adminPassword)
            return Unauthorized(new { error = "Falsches Admin-Passwort." });

        return Ok(new AdminLoginResponse(_jwt.GenerateAdminToken()));
    }

    [HttpGet("users"), Authorize(Roles = "admin")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users.Include(u => u.LicenseKey).OrderByDescending(u => u.CreatedAt).ToListAsync();

        var result = users.Select(u =>
        {
            int? remainingDays = null;
            if (u.ExpiresAt.HasValue)
                remainingDays = Math.Max(0, (int)(u.ExpiresAt.Value - DateTime.UtcNow).TotalDays);

            return new UserDetail(
                u.Id, u.Username, u.CreatedAt, u.ExpiresAt,
                remainingDays, u.IsActive, u.HwidHash != null,
                u.LastLoginAt, u.LicenseKey?.Key, u.ForceLogoutAt,
                u.LastVerifiedAt
            );
        });

        return Ok(result);
    }

    [HttpDelete("users/{id}"), Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.Include(u => u.LicenseKey).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.ForceLogoutAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (user.LicenseKey != null)
        {
            user.LicenseKey.IsUsed = false;
            user.LicenseKey.UsedByUserId = null;
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "User gelöscht." });
    }

    [HttpPost("force-logout/{id}"), Authorize(Roles = "admin")]
    public async Task<IActionResult> ForceLogout(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.ForceLogoutAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "User force logged out." });
    }

    [HttpPost("reset-hwid/{id}"), Authorize(Roles = "admin")]
    public async Task<IActionResult> ResetHwid(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.HwidHash = null;
        user.HwidLockedAt = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "HWID zurückgesetzt." });
    }

    [HttpPost("keys"), Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest req)
    {
        if (req.DurationDays.HasValue && req.DurationDays < 1)
            return BadRequest(new { error = "DurationDays muss > 0 oder null (permanent) sein." });

        var keyStr = KeyGenerator.Generate();
        var expiresAt = KeyGenerator.CalculateExpiry(req.DurationDays);

        var key = new LicenseKey
        {
            Key = keyStr,
            DurationDays = req.DurationDays,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
        };

        _db.LicenseKeys.Add(key);
        await _db.SaveChangesAsync();

        return Ok(new CreateKeyResponse(keyStr, req.DurationDays, expiresAt));
    }

    [HttpGet("keys"), Authorize(Roles = "admin")]
    public async Task<IActionResult> GetKeys()
    {
        var keys = await _db.LicenseKeys.Include(k => k.UsedByUser).OrderByDescending(k => k.CreatedAt).ToListAsync();

        var result = keys.Select(k => new KeyInfo(
            k.Id, k.Key, k.DurationDays, k.IsUsed,
            k.UsedByUser?.Username, k.CreatedAt, k.ExpiresAt
        ));

        return Ok(result);
    }

    [HttpGet("stats"), Authorize(Roles = "admin")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var fiveMinAgo = now.AddMinutes(-5);
        var totalUsers = await _db.Users.CountAsync();
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive && (!u.ExpiresAt.HasValue || u.ExpiresAt > now));
        var expiredUsers = await _db.Users.CountAsync(u => u.ExpiresAt.HasValue && u.ExpiresAt < now);
        var totalKeys = await _db.LicenseKeys.CountAsync();
        var usedKeys = await _db.LicenseKeys.CountAsync(k => k.IsUsed);
        var onlineUsers = await _db.Users.CountAsync(u => u.LastVerifiedAt.HasValue && u.LastVerifiedAt > fiveMinAgo && u.IsActive);
        var onlineUserList = await _db.Users
            .Where(u => u.LastVerifiedAt.HasValue && u.LastVerifiedAt > fiveMinAgo && u.IsActive)
            .OrderByDescending(u => u.LastVerifiedAt)
            .Select(u => new { u.Id, u.Username, u.LastVerifiedAt })
            .ToListAsync();

        return Ok(new
        {
            totalUsers,
            activeUsers,
            expiredUsers,
            totalKeys,
            usedKeys,
            onlineUsers,
            onlineUserList,
        });
    }

    [HttpDelete("keys/{id}"), Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteKey(int id)
    {
        var key = await _db.LicenseKeys.FirstOrDefaultAsync(k => k.Id == id);
        if (key == null) return NotFound();

        if (key.IsUsed)
            return BadRequest(new { error = "Cannot delete a key that is currently in use." });

        _db.LicenseKeys.Remove(key);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Key deleted." });
    }
}
