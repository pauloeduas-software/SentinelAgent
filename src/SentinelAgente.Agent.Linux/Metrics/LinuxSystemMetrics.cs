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

    // Estado persistente para cálculo de velocidade de rede
    private static long _lastRx = 0;
    private static long _lastTx = 0;
    private static DateTime _lastTime = DateTime.MinValue;

    public async Task<MetricsPacket> CollectAsync()
    {
        // 1. RAM (via /proc/meminfo)
        var memInfo = ParseMemInfo();

        // 2. CPU (via delta /proc/stat)
        var cpuUsage = await CalculateCpuUsageAsync();

        // 3. Disco: Nativo .NET (DriveInfo) - Filtro de partições reais
        var disks = DriveInfo.GetDrives()
            .Where(d => d.IsReady && !d.Name.StartsWith("/sys") && !d.Name.StartsWith("/dev") && !d.Name.StartsWith("/run") && !d.Name.StartsWith("/proc"))
            .Select(d => {
                try {
                    long total = d.TotalSize;
                    long free = d.AvailableFreeSpace;
                    
                    if (total <= 0) return null;

                    return new { 
                        name = d.Name, 
                        totalGb = Math.Round(total / 1073741824.0, 2), 
                        usedGb = Math.Round((total - free) / 1073741824.0, 2) 
                    };
                } catch { 
                    return null; 
                }
            })
            .Where(d => d != null)
            .ToList();

        // 4. Processos: Agrupados por Nome (Top 10)
        var topProcesses = System.Diagnostics.Process.GetProcesses()
            .Select(p => {
                try {
                    return new { 
                        name = p.ProcessName, 
                        ramBytes = p.WorkingSet64
                    };
                } catch { return null; }
            })
            .Where(p => p != null)
            .GroupBy(p => p.name)
            .Select(g => new {
                name = g.Key,
                ramMb = Math.Round(g.Sum(x => x.ramBytes) / 1048576.0, 2),
                cpu = 0.0 // Mantendo compatibilidade com a estrutura, foco em RAM agrupada
            })
            .OrderByDescending(p => p.ramMb)
            .Take(10)
            .ToList();

        // 5. Rede: Cálculos de Gb Totais e Kbps Velocidade
        var now = DateTime.UtcNow;
        long currentRx = 0;
        long currentTx = 0;

        try {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                           (n.Name.StartsWith("en") || n.Name.StartsWith("eth") || n.Name.StartsWith("wl")))
                .ToList();

            currentRx = interfaces.Sum(i => i.GetIPv4Statistics().BytesReceived);
            currentTx = interfaces.Sum(i => i.GetIPv4Statistics().BytesSent);
        } catch { }

        // Cálculos de velocidade
        double rxSpeedKbps = 0;
        double txSpeedKbps = 0;

        if (_lastTime != DateTime.MinValue) {
            double secondsPassed = (now - _lastTime).TotalSeconds;
            if (secondsPassed > 0) {
                rxSpeedKbps = Math.Round(((currentRx - _lastRx) * 8.0 / 1000.0) / secondsPassed, 2);
                txSpeedKbps = Math.Round(((currentTx - _lastTx) * 8.0 / 1000.0) / secondsPassed, 2);
            }
        }

        // Atualiza estado para próxima coleta
        _lastRx = currentRx;
        _lastTx = currentTx;
        _lastTime = now;

        var networkData = new {
            totalRxGb = Math.Round((currentRx * 8.0) / 1000000000.0, 2),
            totalTxGb = Math.Round((currentTx * 8.0) / 1000000000.0, 2),
            rxSpeedKbps = rxSpeedKbps < 0 ? 0 : rxSpeedKbps,
            txSpeedKbps = txSpeedKbps < 0 ? 0 : txSpeedKbps
        };

        return new MetricsPacket(
            _hwidGenerator.Generate(),
            Math.Round(cpuUsage, 2),
            memInfo.Total,
            memInfo.Used,
            disks,
            networkData,
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
