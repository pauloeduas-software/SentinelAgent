using System.Security.Cryptography;
using System.Text;
using System.Net.NetworkInformation;

namespace SentinelAgente.Agent.Core.Identity;

public class HwidGenerator(IHardwareProvider hardwareProvider)
{
    private readonly IHardwareProvider _hardwareProvider = hardwareProvider;

    public string Generate()
    {
        // 1. Identificação via Serial Físico Real (DMI/BIOS)
        // Requer privilégios de ROOT para ler de /sys/class/dmi/id/
        var hwid = _hardwareProvider.GetMotherboardId();
        
        if (string.IsNullOrWhiteSpace(hwid))
        {
            throw new Exception("Falha crítica de identificação: Não foi possível ler o ID único de hardware. O Agente deve rodar como ROOT.");
        }

        // Normalização
        hwid = hwid.Replace("-", "").Replace(":", "").Replace(" ", "").ToLowerInvariant();

        // LOG AGRESSIVO PARA QA
        Console.WriteLine($"\n[SENTINEL QA] >>> HWID RAW DETECTADO: {hwid}");

        return ComputeSha256Hash(hwid);
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
