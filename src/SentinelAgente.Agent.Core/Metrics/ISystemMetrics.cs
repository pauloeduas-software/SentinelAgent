using SentinelAgente.Shared.Packets;

namespace SentinelAgente.Agent.Core.Metrics;

/// <summary>
/// Define o contrato para coleta de telemetria de hardware multiplataforma.
/// </summary>
public interface ISystemMetrics
{
    /// <summary>
    /// Coleta o estado atual de CPU, RAM e Disco.
    /// </summary>
    /// <returns>Um MetricsPacket preenchido.</returns>
    Task<MetricsPacket> CollectAsync();
}
