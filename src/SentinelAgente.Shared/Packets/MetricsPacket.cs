using System.Text.Json.Serialization;

namespace SentinelAgente.Shared.Packets;

public record MetricsPacket(
    [property: JsonPropertyName("hwid")] string Hwid,
    [property: JsonPropertyName("cpuUsagePercentage")] double CpuUsagePercentage,
    [property: JsonPropertyName("ramTotalBytes")] long RamTotalBytes,
    [property: JsonPropertyName("ramUsedBytes")] long RamUsedBytes,
    [property: JsonPropertyName("disks")] object Disks,
    [property: JsonPropertyName("network")] object Network,
    [property: JsonPropertyName("topProcesses")] object TopProcesses
);
