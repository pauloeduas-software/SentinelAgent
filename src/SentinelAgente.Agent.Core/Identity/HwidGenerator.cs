using System.Security.Cryptography;
using System.Text;
using System.Net.NetworkInformation;

namespace SentinelAgente.Agent.Core.Identity;

public class HwidGenerator(IHardwareProvider hardwareProvider)
{
    private readonly IHardwareProvider _hardwareProvider = hardwareProvider;

    public string Generate()
    {
        string rawIdentifier = string.Empty;

        // 1. Tenta identificar via Serial Físico (DMI)
        var dmiSerial = _hardwareProvider.GetMotherboardId();
        
        if (!string.IsNullOrWhiteSpace(dmiSerial))
        {
            rawIdentifier = dmiSerial;
        }
        else
        {
            // 2. Fallback para MAC Address puramente físico
            rawIdentifier = GetPhysicalMacAddress();
        }

        // 3. Fallback final: Nome da Máquina (Extremo)
        if (string.IsNullOrWhiteSpace(rawIdentifier))
        {
            rawIdentifier = Environment.MachineName;
        }

        // Normalização
        rawIdentifier = rawIdentifier.Replace("-", "").Replace(":", "").Replace(" ", "").ToLowerInvariant();

        // LOG AGRESSIVO PARA QA
        Console.WriteLine($"\n[SENTINEL QA] >>> HWID RAW DETECTADO: {rawIdentifier}");

        return ComputeSha256Hash(rawIdentifier);
    }

    private string GetPhysicalMacAddress()
    {
        try
        {
            // Filtra apenas interfaces físicas reais (Ethernet e Wi-Fi)
            // Ignora: docker, veth, lo, br, tun, tap
            string[] allowedPrefixes = { "en", "eth", "wl" };

            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && 
                            n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            allowedPrefixes.Any(p => n.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(n => n.Name)
                .FirstOrDefault();

            return nic?.GetPhysicalAddress().ToString() ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
