using System.Text.Json.Serialization;

namespace SentinelAgente.Shared.Packets;

/// <summary>
/// Payload de telemetria contendo o estado atual dos recursos de hardware.
/// </summary>
public record MetricsPacket(
    [property: JsonPropertyName("hwid")] string Hwid,
    [property: JsonPropertyName("cpuUsagePercentage")] double CpuUsagePercentage,
    [property: JsonPropertyName("ramTotalBytes")] long RamTotalBytes,
    [property: JsonPropertyName("ramUsedBytes")] long RamUsedBytes,
    [property: JsonPropertyName("disks")] Dictionary<string, double> Disks
);
