using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloxHive.API.Models;

public class LicenseKey
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Key { get; set; } = "";

    public int? DurationDays { get; set; }

    public bool IsUsed { get; set; }

    public int? UsedByUserId { get; set; }

    [ForeignKey(nameof(UsedByUserId))]
    public User? UsedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }
}
