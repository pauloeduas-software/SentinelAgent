using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Storage;
using SentinelAgente.Shared.Packets;

namespace SentinelAgente.Agent.Core.Communication;

/// <summary>
/// Cliente WebSocket resiliente responsável pela comunicação bidirecional com o servidor.
/// </summary>
public class WssClient(
    string serverUri, 
    HwidGenerator hwidGenerator, 
    OfflineBuffer<MetricsPacket> offlineBuffer) : IDisposable
{
    private readonly Uri _serverUri = new(serverUri);
    private readonly HwidGenerator _hwidGenerator = hwidGenerator;
    private readonly OfflineBuffer<MetricsPacket> _offlineBuffer = offlineBuffer;
    
    private ClientWebSocket? _webSocket;
    private int _retryAttempt = 0;

    /// <summary>
    /// Inicia o ciclo de vida da conexão, mantendo o agente online indefinidamente.
    /// </summary>
    public async Task StartConnectionLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Garante que o WebSocket anterior seja descartado antes de uma nova tentativa
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();

                // 1. Tenta a conexão física
                await _webSocket.ConnectAsync(_serverUri, ct);
                
                // Sucesso: Reseta o contador de backoff
                _retryAttempt = 0;

                // 2. Realiza o Handshake obrigatório para identificação do HWID
                await SendHandshakeAsync(ct);

                // 3. Descarrega métricas que foram coletadas enquanto o agente estava offline
                await FlushOfflineBufferAsync(ct);

                // 4. Entra no loop de escuta para receber comandos (Lock, Reboot, etc.)
                // Este método bloqueia a execução até que a conexão caia
                await ReceiveLoopAsync(ct);
            }
            catch (Exception)
            {
                // Em caso de falha de rede ou servidor indisponível:
                _retryAttempt++;
                
                var delay = BackoffPolicy.CalculateDelay(_retryAttempt);
                
                // Aguarda o tempo calculado pelo Backoff antes da próxima tentativa
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>
    /// Envia o pacote de identificação inicial.
    /// </summary>
    private async Task SendHandshakeAsync(CancellationToken ct)
    {
        var packet = new HandshakePacket(
            _hwidGenerator.Generate(),
            Environment.MachineName,
            Environment.OSVersion.ToString(),
            "1.0.0-stable" // Versão do binário (Mock)
        );

        await SendInternalAsync(packet, ct);
    }

    /// <summary>
    /// Tenta enviar uma métrica em tempo real. Se falhar, armazena no buffer offline.
    /// </summary>
    public async Task SendMetricsAsync(MetricsPacket packet, CancellationToken ct = default)
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            try
            {
                await SendInternalAsync(packet, ct);
                return;
            }
            catch { /* Fallback para o buffer em caso de erro de escrita */ }
        }

        _offlineBuffer.Enqueue(packet);
    }

    /// <summary>
    /// Envia todos os pacotes acumulados no buffer offline após a reconexão.
    /// </summary>
    private async Task FlushOfflineBufferAsync(CancellationToken ct)
    {
        while (_offlineBuffer.TryDequeue(out var packet) && packet != null)
        {
            if (_webSocket?.State != WebSocketState.Open) break;
            await SendInternalAsync(packet, ct);
        }
    }

    /// <summary>
    /// Loop de recepção que aguarda mensagens do servidor.
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[1024 * 8]; // Buffer de 8KB para pacotes JSON

        while (_webSocket?.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Desconectado pelo servidor", ct);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                // TODO: Integrar com o CommandDispatcher para executar ações locais
            }
        }
    }

    /// <summary>
    /// Encerra a conexão WebSocket de forma amigável com o servidor.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_webSocket != null && (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived))
        {
            try
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, 
                    "Agent shutting down", 
                    CancellationToken.None);
            }
            catch { /* Ignora se a conexão já foi perdida */ }
        }
    }

    /// <summary>
    /// Serializa e envia um pacote via WebSocket, envelopado para o roteamento no servidor.
    /// </summary>
    private async Task SendInternalAsync<T>(T packet, CancellationToken ct)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        // Determina o tipo do pacote para o envelope do servidor
        string packetType = typeof(T).Name switch
        {
            var name when name.Contains("Handshake") => "Handshake",
            var name when name.Contains("Metrics") => "Telemetry",
            _ => "Unknown"
        };

        // Cria o envelope solicitado pelo backend
        var envelope = new
        {
            Type = packetType,
            Payload = packet
        };

        var json = JsonSerializer.Serialize(envelope);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        await _webSocket.SendAsync(
            new ArraySegment<byte>(bytes), 
            WebSocketMessageType.Text, 
            true, 
            ct);
    }

    public void Dispose()
    {
        _webSocket?.Dispose();
        GC.SuppressFinalize(this);
    }
}
