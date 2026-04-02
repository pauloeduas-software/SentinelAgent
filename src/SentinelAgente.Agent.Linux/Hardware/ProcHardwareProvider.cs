using SentinelAgente.Agent.Core.Identity;

namespace SentinelAgente.Agent.Linux.Hardware;

public class ProcHardwareProvider : IHardwareProvider
{
    public string? GetMotherboardId()
    {
        // PRIORIDADE 1: Serial do Produto (Service Tag)
        var productSerial = ReadSysFile("/sys/class/dmi/id/product_serial");
        if (IsValidHardwareSerial(productSerial)) return productSerial?.Trim();

        // PRIORIDADE 2: Serial da Placa-mãe
        var boardSerial = ReadSysFile("/sys/class/dmi/id/board_serial");
        if (IsValidHardwareSerial(boardSerial)) return boardSerial?.Trim();

        return null;
    }

    private static bool IsValidHardwareSerial(string? serial)
    {
        if (string.IsNullOrWhiteSpace(serial)) return false;
        
        string s = serial.Trim();
        string[] invalidPlaceholders = { 
            "None", "Default string", "Not Specified", 
            "To be filled by O.E.M.", "System Serial Number", 
            "00000000", "Unknown" 
        };

        return !invalidPlaceholders.Any(p => s.Equals(p, StringComparison.OrdinalIgnoreCase)) && s.Length > 3;
    }

    public string? GetCpuId()
    {
        var cpuInfo = ReadSysFile("/proc/cpuinfo");
        if (string.IsNullOrWhiteSpace(cpuInfo)) return null;

        var lines = cpuInfo.Split('\n');
        var modelName = lines.FirstOrDefault(l => l.Contains("model name", StringComparison.OrdinalIgnoreCase))?.Split(':').LastOrDefault()?.Trim();
        return modelName;
    }

    private static string? ReadSysFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path).Trim();
        }
        catch { return null; }
    }
}
