using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SentinelAgente.Agent.Linux.Identity;

public class LinuxInventoryProvider : Core.Identity.IInventoryProvider
{
    public string GetCpuModel()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/cpuinfo");
            var modelLine = lines.FirstOrDefault(l => l.Contains("model name", StringComparison.OrdinalIgnoreCase));
            return modelLine?.Split(':').LastOrDefault()?.Trim() ?? "Unknown Linux CPU";
        }
        catch { return "Unknown Linux CPU"; }
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

    public List<string> GetInstalledSoftware() => new() { "Linux Software Inventory (Pending dpkg/rpm parser)" };
}
