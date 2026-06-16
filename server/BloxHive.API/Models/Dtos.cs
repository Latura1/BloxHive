namespace BloxHive.API.Models;

public record RegisterRequest(string Key, string Username, string Password);

public record LoginRequest(string Username, string Password, string Hwid);

public record LoginResponse(string Token, UserInfo User);

public record UserInfo(int Id, string Username, DateTime? ExpiresAt, bool IsActive, DateTime? ForceLogoutAt, DateTime CreatedAt, DateTime? LastVerifiedAt);

public record CreateKeyRequest(int? DurationDays);

public record CreateKeyResponse(string Key, int? DurationDays, DateTime? ExpiresAt);

public record AdminLoginRequest(string Password);

public record AdminLoginResponse(string Token);

public record KeyInfo(int Id, string Key, int? DurationDays, bool IsUsed, string? UsedBy, DateTime CreatedAt, DateTime? ExpiresAt);

public record UserDetail(int Id, string Username, DateTime CreatedAt, DateTime? ExpiresAt, int? RemainingDays, bool IsActive, bool HasHwid, DateTime? LastLoginAt, string? UsedKey, DateTime? ForceLogoutAt, DateTime? LastVerifiedAt);
