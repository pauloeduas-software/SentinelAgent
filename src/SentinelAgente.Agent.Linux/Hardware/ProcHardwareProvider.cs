using SentinelAgente.Agent.Core.Identity;

namespace SentinelAgente.Agent.Linux.Hardware;

/// <summary>
/// Provedor de hardware específico para Linux, extraindo dados diretamente do sysfs e procfs.
/// </summary>
public class ProcHardwareProvider : IHardwareProvider
{
    /// <summary>
    /// Recupera a identidade da placa-mãe via DMI (Desktop Management Interface).
    /// </summary>
    public string? GetMotherboardId()
    {
        // Tenta o serial da placa primeiro
        var serial = ReadSysFile("/sys/class/dmi/id/board_serial");
        
        // Se falhar (comum em VMs), tenta o UUID do produto
        return !string.IsNullOrWhiteSpace(serial) ? serial : ReadSysFile("/sys/class/dmi/id/product_uuid");
    }

    /// <summary>
    /// Recupera o identificador do processador analisando o /proc/cpuinfo.
    /// </summary>
    public string? GetCpuId()
    {
        var cpuInfo = ReadSysFile("/proc/cpuinfo");
        if (string.IsNullOrWhiteSpace(cpuInfo)) return null;

        var lines = cpuInfo.Split('\n');

        // 1. Tenta encontrar a linha 'Serial' (Padrão em ARM/Raspberry Pi)
        var serialLine = lines.FirstOrDefault(l => l.StartsWith("Serial", StringComparison.OrdinalIgnoreCase));
        if (serialLine != null)
        {
            return serialLine.Split(':').LastOrDefault()?.Trim();
        }

        // 2. Fallback para x86: Combina Model Name + Stepping para gerar uma assinatura de modelo
        var modelName = lines.FirstOrDefault(l => l.Contains("model name", StringComparison.OrdinalIgnoreCase))?.Split(':').LastOrDefault()?.Trim();
        var stepping = lines.FirstOrDefault(l => l.Contains("stepping", StringComparison.OrdinalIgnoreCase))?.Split(':').LastOrDefault()?.Trim();

        if (!string.IsNullOrWhiteSpace(modelName))
        {
            return $"{modelName}-{stepping ?? "0"}".Replace(" ", "");
        }

        return null;
    }

    /// <summary>
    /// Recupera o serial do disco físico, suportando SATA (sda) e NVMe (nvme0).
    /// </summary>
    public string? GetDiskSerialNumber()
    {
        // Tenta o disco SATA/SCSI padrão (sda)
        var sdaSerial = ReadSysFile("/sys/block/sda/device/serial");
        if (!string.IsNullOrWhiteSpace(sdaSerial)) return sdaSerial;

        // Tenta o disco NVMe padrão (nvme0n1)
        var nvmeSerial = ReadSysFile("/sys/block/nvme0n1/device/serial");
        return nvmeSerial;
    }

    /// <summary>
    /// Método auxiliar para leitura segura de arquivos virtuais do sistema.
    /// </summary>
    private static string? ReadSysFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;

            // No Linux, arquivos no /sys e /proc devem ser lidos de uma vez. 
            // Eles sempre terminam com uma quebra de linha (\n) que deve ser removida.
            var content = File.ReadAllText(path).Trim();
            
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch
        {
            // Silencia erros de permissão (ex: board_serial as vezes exige root) ou arquivos inacessíveis
            return null;
        }
    }
}
