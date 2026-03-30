using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SentinelAgente.Agent.Core.Communication;
using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Storage;
using SentinelAgente.Agent.Core.Metrics;
using SentinelAgente.Shared.Packets;
using SentinelAgente.Agent.Worker;

// Namespaces específicos por SO (Apenas Linux para ambiente Zorin OS)
using SentinelAgente.Agent.Linux.Hardware;
using SentinelAgente.Agent.Linux.Metrics;

var builder = Host.CreateApplicationBuilder(args);

// 1. Configuração do Buffer Offline (10 métricas em RAM)
builder.Services.AddSingleton(new OfflineBuffer<MetricsPacket>(10));

// 2. Registro de Hardware e Sensores (Focado em Linux para Testes Locais)
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddSingleton<IHardwareProvider, ProcHardwareProvider>();
    builder.Services.AddSingleton<ISystemMetrics, LinuxSystemMetrics>();
}
else
{
    // Bloqueio explícito para evitar erros de compilação/execução em outras plataformas neste estágio
    throw new PlatformNotSupportedException("Este build de testes suporta apenas Linux.");
}

// 3. Registro do Gerador de HWID e Cliente WebSocket
builder.Services.AddSingleton<HwidGenerator>();
builder.Services.AddSingleton(sp => 
{
    var hwidGen = sp.GetRequiredService<HwidGenerator>();
    var buffer = sp.GetRequiredService<OfflineBuffer<MetricsPacket>>();
    
    // URL do MockServer Local
    const string serverUrl = "ws://localhost:5000/agent-hub"; 
    
    return new WssClient(serverUrl, hwidGen, buffer);
});

// 4. Registro do Worker Service (O Orquestrador)
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();
