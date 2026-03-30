using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SentinelAgente.Shared.Packets;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000"); // Porta fixa para o agente conectar

var app = builder.Build();

app.UseWebSockets();

app.Map("/agent-hub", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

    Console.WriteLine($"\n[INFO]: Agente Conectado: {clientId}");

    var buffer = new byte[1024 * 8];
    try
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine($"[INFO]: Agente {clientId} solicitou encerramento.");
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                // Tenta Identificar o Tipo de Pacote para um log mais rico
                if (json.Contains("cpuUsagePercentage"))
                {
                    var metrics = JsonSerializer.Deserialize<MetricsPacket>(json);
                    Console.WriteLine($"[TELEMETRIA] HWID: {metrics?.Hwid.Substring(0, 8)} | CPU: {metrics?.CpuUsagePercentage}% | RAM: {metrics?.RamUsedBytes / 1024 / 1024}MB");
                }
                else if (json.Contains("agentVersion"))
                {
                    var handshake = JsonSerializer.Deserialize<HandshakePacket>(json);
                    Console.WriteLine($"[HANDSHAKE] Agente: {handshake?.Hostname} | OS: {handshake?.OsVersion} | HWID: {handshake?.Hwid.Substring(0, 8)}");
                }
                else
                {
                    Console.WriteLine($"[RECEBIDO]: {json}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO]: Conexão perdida com {clientId}. Motivo: {ex.Message}");
    }
    finally
    {
        Console.WriteLine($"[INFO]: Conexão Encerrada para {clientId}.\n");
    }
});

Console.WriteLine("🚀 Mock ITAM Server Rodando em http://localhost:5000/agent-hub");
app.Run();
