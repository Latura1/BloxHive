using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloxHive.API.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(128)]
    public string? HwidHash { get; set; }

    public DateTime? HwidLockedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public DateTime? LoginSessionStart { get; set; }
    public DateTime? ForceLogoutAt { get; set; }
    public DateTime? LastVerifiedAt { get; set; }

    public int? LicenseKeyId { get; set; }

    [ForeignKey(nameof(LicenseKeyId))]
    public LicenseKey? LicenseKey { get; set; }
}
