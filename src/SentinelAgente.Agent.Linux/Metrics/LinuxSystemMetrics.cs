using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Metrics;
using SentinelAgente.Shared.Packets;

namespace SentinelAgente.Agent.Linux.Metrics;

/// <summary>
/// Sensor de telemetria específico para Linux com tratamento robusto de permissões de disco.
/// </summary>
public class LinuxSystemMetrics(HwidGenerator hwidGenerator) : ISystemMetrics
{
    private readonly HwidGenerator _hwidGenerator = hwidGenerator;

    public async Task<MetricsPacket> CollectAsync()
    {
        // 1. RAM (via /proc/meminfo)
        var memInfo = ParseMemInfo();

        // 2. CPU (via delta /proc/stat)
        var cpuUsage = await CalculateCpuUsageAsync();

        // 3. Disco: Nativo .NET (DriveInfo) - Filtro de partições reais
        var disks = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.TotalSize > 0 && !d.Name.StartsWith("/sys") && !d.Name.StartsWith("/dev") && !d.Name.StartsWith("/run") && !d.Name.StartsWith("/proc"))
            .Select(d => {
                try {
                    return new { 
                        name = d.Name, 
                        totalGb = Math.Round(d.TotalSize / 1073741824.0, 2), 
                        usedGb = Math.Round((d.TotalSize - d.AvailableFreeSpace) / 1073741824.0, 2) 
                    };
                } catch { return null; }
            })
            .Where(d => d != null)
            .ToList();

        // 4. Processos: Top 15 Consumo de RAM
        var topProcesses = System.Diagnostics.Process.GetProcesses()
            .OrderByDescending(p => p.WorkingSet64)
            .Take(15)
            .Select(p => {
                try {
                    return new { 
                        pid = p.Id, 
                        name = p.ProcessName, 
                        ramMb = Math.Round(p.WorkingSet64 / 1048576.0, 2) 
                    };
                } catch { return null; }
            })
            .Where(p => p != null)
            .ToList();

        // 5. Rede: Coleta de Interfaces Físicas
        var network = new { bytesReceived = 0L, bytesSent = 0L };
        try {
            var stats = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                           (n.Name.StartsWith("en") || n.Name.StartsWith("eth") || n.Name.StartsWith("wl")))
                .Select(n => n.GetIPv4Statistics())
                .ToList();

            network = new { 
                bytesReceived = stats.Sum(s => s.BytesReceived), 
                bytesSent = stats.Sum(s => s.BytesSent) 
            };
        } catch { /* Fallback para zero em caso de erro */ }

        return new MetricsPacket(
            _hwidGenerator.Generate(),
            Math.Round(cpuUsage, 2),
            memInfo.Total,
            memInfo.Used,
            disks,
            network,
            topProcesses
        );
    }

    private static (long Total, long Used) ParseMemInfo()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            long total = 0, available = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:")) total = ExtractKb(line) * 1024;
                if (line.StartsWith("MemAvailable:")) available = ExtractKb(line) * 1024;
            }

            return (total, total - available);
        }
        catch { return (0, 0); }
    }

    private static async Task<double> CalculateCpuUsageAsync()
    {
        try
        {
            var (total1, idle1) = GetCpuTicks();
            await Task.Delay(500);
            var (total2, idle2) = GetCpuTicks();

            double totalDelta = total2 - total1;
            double idleDelta = idle2 - idle1;

            if (totalDelta == 0) return 0;

            return (1.0 - (idleDelta / totalDelta)) * 100.0;
        }
        catch { return 0; }
    }

    private static (long Total, long Idle) GetCpuTicks()
    {
        var line = File.ReadLines("/proc/stat").First();
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Formato: cpu user nice system idle iowait irq softirq steal guest guest_nice
        long idle = long.Parse(parts[4]) + long.Parse(parts[5]); // idle + iowait
        long total = parts.Skip(1).Select(long.Parse).Sum();

        return (total, idle);
    }

    private static long ExtractKb(string line) => 
        long.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
}
