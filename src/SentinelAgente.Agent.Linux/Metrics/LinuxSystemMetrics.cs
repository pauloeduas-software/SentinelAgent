using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Metrics;
using SentinelAgente.Shared.Packets;

namespace SentinelAgente.Agent.Linux.Metrics;

/// <summary>
/// Sensor de telemetria específico para Linux.
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

        // 3. Disco (Cálculo de % de uso)
        var diskUsage = DriveInfo.GetDrives()
            .Where(d => d.IsReady && (d.DriveFormat == "ext4" || d.DriveFormat == "xfs"))
            .ToDictionary(
                d => d.Name, 
                d => Math.Round(((double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize) * 100, 2)
            );

        return new MetricsPacket(
            _hwidGenerator.Generate(),
            Math.Round(cpuUsage, 2),
            memInfo.Total,
            memInfo.Used,
            diskUsage
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
