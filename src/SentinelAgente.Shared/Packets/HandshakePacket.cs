using System.Text.Json.Serialization;

namespace SentinelAgente.Shared.Packets;

/// <summary>
/// Pacote inicial enviado pelo Agente para identificação e registro no servidor.
/// </summary>
public record HandshakePacket(
    [property: JsonPropertyName("hwid")] string Hwid,
    [property: JsonPropertyName("hostname")] string Hostname,
    [property: JsonPropertyName("osVersion")] string OsVersion,
    [property: JsonPropertyName("agentVersion")] string AgentVersion
);
