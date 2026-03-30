using System.Text.Json.Serialization;

namespace SentinelAgente.Shared.Packets;

/// <summary>
/// Comando enviado do Servidor para o Agente executar uma ação específica.
/// </summary>
public record CommandPacket(
    [property: JsonPropertyName("commandId")] string CommandId,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("payload")] string? Payload
);
