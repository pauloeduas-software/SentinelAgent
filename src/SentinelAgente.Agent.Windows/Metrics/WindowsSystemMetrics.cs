using System.Diagnostics;
using System.Runtime.InteropServices;
using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Metrics;
using SentinelAgente.Shared.Packets;

namespace SentinelAgente.Agent.Windows.Metrics;

/// <summary>
/// Sensor de telemetria específico para Windows.
/// </summary>
public class WindowsSystemMetrics(HwidGenerator hwidGenerator) : ISystemMetrics
{
    private readonly HwidGenerator _hwidGenerator = hwidGenerator;
    private readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");

    public async Task<MetricsPacket> CollectAsync()
    {
        // 1. CPU (Média entre duas leituras)
        _cpuCounter.NextValue();
        await Task.Delay(500);
        double cpuUsage = _cpuCounter.NextValue();

        // 2. RAM (Via P/Invoke GlobalMemoryStatusEx)
        GetMemoryInfo(out long totalRam, out long usedRam);

        // 3. Discos (Drives Fixos - Cálculo de % de uso)
        var diskUsage = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .ToDictionary(
                d => d.Name, 
                d => Math.Round(((double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize) * 100, 2)
            );

        return new MetricsPacket(
            _hwidGenerator.Generate(),
            Math.Round(cpuUsage, 2),
            totalRam,
            usedRam,
            diskUsage
        );
    }

    private static void GetMemoryInfo(out long totalBytes, out long usedBytes)
    {
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
            totalBytes = (long)memStatus.ullTotalPhys;
            usedBytes = (long)(memStatus.ullTotalPhys - memStatus.ullAvailPhys);
        }
        else
        {
            totalBytes = 0;
            usedBytes = 0;
        }
    }

    #region P/Invoke Kernel32
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX() => dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    #endregion
}
