using System.Text.Json.Serialization;

namespace SentinelAgente.Shared.Packets;

/// <summary>
/// Confirmação de execução enviada pelo Agente após processar um CommandPacket.
/// </summary>
public record AckPacket(
    [property: JsonPropertyName("commandId")] string CommandId,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage
);
