using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace BloxHive.Services;

public static class HwidService
{
    public static string Generate()
    {
        var parts = new List<string>();

        try { parts.Add(Environment.MachineName); } catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("ProductId") is string productId)
                parts.Add(productId);
        }
        catch { }

        try
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => n.GetPhysicalAddress().ToString())
                .Where(m => !string.IsNullOrEmpty(m));
            foreach (var mac in nics)
                parts.Add(mac);
        }
        catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\Scsi\Scsi Port 0\Scsi Bus 0\Target Id 0\Logical Unit Id 0");
            if (key?.GetValue("SerialNumber") is string serial)
                parts.Add(serial);
        }
        catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (key?.GetValue("ProcessorNameString") is string cpu)
                parts.Add(cpu);
        }
        catch { }

        var input = string.Join("|", parts.Where(p => !string.IsNullOrEmpty(p)));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLower();
    }
}
