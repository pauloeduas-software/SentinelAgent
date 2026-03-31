using System.Text.Json.Serialization;

namespace SentinelAgente.Shared.Packets;

public record HandshakePacket(
    [property: JsonPropertyName("hwid")] string Hwid,
    [property: JsonPropertyName("hostname")] string Hostname,
    [property: JsonPropertyName("osVersion")] string OsVersion,
    [property: JsonPropertyName("agentVersion")] string AgentVersion,
    [property: JsonPropertyName("macAddress")] string MacAddress,
    [property: JsonPropertyName("localIp")] string LocalIp,
    [property: JsonPropertyName("cpuModel")] string CpuModel,
    [property: JsonPropertyName("installedSoftware")] List<string> InstalledSoftware
);
