using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentinelAgente.Agent.Core.Communication;
using SentinelAgente.Agent.Core.Metrics;

namespace SentinelAgente.Agent.Worker;

/// <summary>
/// Worker Service responsável por orquestrar o ciclo de vida do Agente ITAM.
/// </summary>
public class AgentWorker(
    ILogger<AgentWorker> logger,
    WssClient wssClient,
    ISystemMetrics systemMetrics) : BackgroundService
{
    private readonly ILogger<AgentWorker> _logger = logger;
    private readonly WssClient _wssClient = wssClient;
    private readonly ISystemMetrics _systemMetrics = systemMetrics;

    /// <summary>
    /// Execução principal do serviço em background.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Asset Manager Agent iniciado em {time}.", DateTimeOffset.Now);

        // 1. Inicia o loop de conexão em background (WSS)
        // Não damos 'await' aqui para permitir que o loop de telemetria rode em paralelo
        var connectionTask = _wssClient.StartConnectionLoopAsync(stoppingToken);

        // 2. Loop de coleta e envio de telemetria
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("📊 Coletando métricas de hardware...");
                    
                    var packet = await _systemMetrics.CollectAsync();
                    
                    // O SendMetricsAsync já trata internamente o fallback para o buffer offline
                    // caso a conexão esteja em processo de reconexão.
                    await _wssClient.SendMetricsAsync(packet, stoppingToken);

                    _logger.LogInformation("📊 Telemetria enviada: CPU {cpu}%, RAM {ram}MB.", 
                        packet.CpuUsagePercentage, 
                        (packet.RamUsedBytes / 1024 / 1024));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ Erro durante o ciclo de coleta de métricas.");
                }

                // Aguarda 30 segundos antes da próxima coleta
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ Shutdown do ciclo de telemetria solicitado.");
        }

        // Aguarda a finalização do loop de conexão para garantir um shutdown limpo
        await connectionTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Parando serviço de monitoramento e desconectando do servidor...");
        
        try
        {
            await _wssClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "⚠️ Erro ao desconectar WebSocket graciosamente.");
        }

        await base.StopAsync(cancellationToken);
    }
}
