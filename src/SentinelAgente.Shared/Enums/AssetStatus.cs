using System.Text.Json.Serialization;

namespace SentinelAgente.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetStatus
{
    Available,   // Disponível para uso
    Allocated,   // Vinculado a um usuário/posto
    Maintenance, // Em manutenção técnica
    Scrapped     // Ativo baixado/sucata
}
