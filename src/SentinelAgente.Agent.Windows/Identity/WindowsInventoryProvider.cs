using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Win32;

namespace SentinelAgente.Agent.Windows.Identity;

public class WindowsInventoryProvider : Core.Identity.IInventoryProvider
{
    public string GetCpuModel()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return obj["Name"]?.ToString()?.Trim() ?? "Unknown CPU";
            }
        }
        catch { }
        return "Unknown Windows CPU";
    }

    public string GetLocalIp() => 
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(i => i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(i => i.GetIPProperties().UnicastAddresses)
            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString() ?? "127.0.0.1";

    public string GetMacAddress() => 
        NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(i => i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            ?.GetPhysicalAddress().ToString() ?? "000000000000";

    public List<string> GetInstalledSoftware()
    {
        var software = new HashSet<string>();
        string[] keys = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var keyPath in keys)
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath);
            if (key == null) continue;

            foreach (var subkeyName in key.GetSubKeyNames())
            {
                using var subkey = key.OpenSubKey(subkeyName);
                var name = subkey?.GetValue("DisplayName")?.ToString();
                if (!string.IsNullOrWhiteSpace(name)) software.Add(name);
            }
        }

        return software.OrderBy(s => s).ToList();
    }
}
