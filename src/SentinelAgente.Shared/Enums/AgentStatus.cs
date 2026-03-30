using System.Text.Json.Serialization;

namespace SentinelAgente.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentStatus
{
    Online,     // Comunicando em tempo real
    Offline,    // Sem comunicação ativa
    Quarantine  // HWID novo aguardando homologação da TI
}
