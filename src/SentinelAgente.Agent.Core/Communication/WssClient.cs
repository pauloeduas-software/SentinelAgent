using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Storage;
using SentinelAgente.Agent.Core.Commands;
using SentinelAgente.Shared.Packets;

namespace SentinelAgente.Agent.Core.Communication;

/// <summary>
/// Cliente WebSocket resiliente responsável pela comunicação bidirecional com o servidor.
/// </summary>
public class WssClient(
    string serverUri, 
    HwidGenerator hwidGenerator, 
    OfflineBuffer<MetricsPacket> offlineBuffer,
    IInventoryProvider inventoryProvider) : IDisposable
{
    private readonly Uri _serverUri = new(serverUri);
    private readonly HwidGenerator _hwidGenerator = hwidGenerator;
    private readonly OfflineBuffer<MetricsPacket> _offlineBuffer = offlineBuffer;
    private readonly IInventoryProvider _inventoryProvider = inventoryProvider;
    
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
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();

                await _webSocket.ConnectAsync(_serverUri, ct);
                _retryAttempt = 0;

                await SendHandshakeAsync(ct);
                await FlushOfflineBufferAsync(ct);
                await ReceiveLoopAsync(ct);
            }
            catch (Exception)
            {
                _retryAttempt++;
                var delay = BackoffPolicy.CalculateDelay(_retryAttempt);
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>
    /// Envia o pacote de identificação inicial com inventário profundo.
    /// </summary>
    private async Task SendHandshakeAsync(CancellationToken ct)
    {
        var packet = new HandshakePacket(
            _hwidGenerator.Generate(),
            Environment.MachineName,
            RuntimeInformation.OSDescription,
            "2.0.0-sentinel",
            _inventoryProvider.GetMacAddress(),
            _inventoryProvider.GetLocalIp(),
            _inventoryProvider.GetCpuModel(),
            _inventoryProvider.GetInstalledSoftware()
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
            catch { }
        }

        _offlineBuffer.Enqueue(packet);
    }

    private async Task FlushOfflineBufferAsync(CancellationToken ct)
    {
        while (_offlineBuffer.TryDequeue(out var packet) && packet != null)
        {
            if (_webSocket?.State != WebSocketState.Open) break;
            await SendInternalAsync(packet, ct);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[1024 * 8];

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
                
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Detecta se a mensagem é um Comando da API
                    if (root.TryGetProperty("Type", out var typeProp) && typeProp.GetString() == "Command")
                    {
                        var payload = root.GetProperty("Payload");
                        var action = payload.GetProperty("Action").GetString();

                        if (!string.IsNullOrEmpty(action))
                        {
                            CommandDispatcher.ExecuteCommand(action);
                        }
                    }
                }
                catch { /* Ignora JSON malformado */ }
            }
        }
    }

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
            catch { }
        }
    }

    private async Task SendInternalAsync<T>(T packet, CancellationToken ct)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        string packetType = typeof(T).Name switch
        {
            var name when name.Contains("Handshake") => "Handshake",
            var name when name.Contains("Metrics") => "Telemetry",
            _ => "Unknown"
        };

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
