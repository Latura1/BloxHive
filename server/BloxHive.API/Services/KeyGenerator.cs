using System.Security.Cryptography;

namespace BloxHive.API.Services;

public static class KeyGenerator
{
    public static string Generate()
    {
        var bytes = new byte[12];
        RandomNumberGenerator.Fill(bytes);
        var hex = Convert.ToHexString(bytes).ToUpper();
        return $"BLOX-{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
    }

    public static DateTime? CalculateExpiry(int? durationDays)
    {
        return durationDays.HasValue
            ? DateTime.UtcNow.AddDays(durationDays.Value)
            : null;
    }
}
