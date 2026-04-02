using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SentinelAgente.Agent.Core.Communication;
using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Storage;
using SentinelAgente.Agent.Core.Metrics;
using SentinelAgente.Shared.Packets;
using SentinelAgente.Agent.Worker;

// Namespaces nativos Linux
using SentinelAgente.Agent.Linux.Identity;
using SentinelAgente.Agent.Linux.Hardware;
using SentinelAgente.Agent.Linux.Metrics;

var builder = Host.CreateApplicationBuilder(args);

// Configuração nativa para Systemd (Linux)
builder.Services.AddSystemd();

// 1. Configuração do Buffer Offline (10 métricas em RAM)
builder.Services.AddSingleton(new OfflineBuffer<MetricsPacket>(10));

// 2. Registro de Hardware, Sensores e Inventário (Linux Puro)
builder.Services.AddSingleton<IHardwareProvider, ProcHardwareProvider>();
builder.Services.AddSingleton<ISystemMetrics, LinuxSystemMetrics>();
builder.Services.AddSingleton<IInventoryProvider, LinuxInventoryProvider>();

// 3. Registro do Gerador de HWID e Cliente WebSocket
builder.Services.AddSingleton<HwidGenerator>();
builder.Services.AddSingleton(sp => 
{
    var hwidGen = sp.GetRequiredService<HwidGenerator>();
    var buffer = sp.GetRequiredService<OfflineBuffer<MetricsPacket>>();
    var inventory = sp.GetRequiredService<IInventoryProvider>();
    
    // URL do Servidor Sentinel
    const string serverUrl = "ws://localhost:5000/agent-hub"; 
    
    return new WssClient(serverUrl, hwidGen, buffer, inventory);
});

// 4. Registro do Worker Service (O Orquestrador)
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();
