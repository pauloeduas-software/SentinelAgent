using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SentinelAgente.Agent.Core.Communication;
using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Storage;
using SentinelAgente.Agent.Core.Metrics;
using SentinelAgente.Shared.Packets;
using SentinelAgente.Agent.Worker;

// Namespaces de Identidade e Inventário
using SentinelAgente.Agent.Linux.Identity;

#if WINDOWS
using SentinelAgente.Agent.Windows.Identity;
using SentinelAgente.Agent.Windows.Hardware;
using SentinelAgente.Agent.Windows.Metrics;
#endif

// Namespaces de Hardware e Métricas específicos por SO
using SentinelAgente.Agent.Linux.Hardware;
using SentinelAgente.Agent.Linux.Metrics;

var builder = Host.CreateApplicationBuilder(args);

// 1. Configuração do Buffer Offline (10 métricas em RAM)
builder.Services.AddSingleton(new OfflineBuffer<MetricsPacket>(10));

// 2. Registro de Hardware, Sensores e Inventário (Agnóstico via DI)
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddSingleton<IHardwareProvider, ProcHardwareProvider>();
    builder.Services.AddSingleton<ISystemMetrics, LinuxSystemMetrics>();
    builder.Services.AddSingleton<IInventoryProvider, LinuxInventoryProvider>();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
#if WINDOWS
    // Registro para ambiente Windows (WMI + Registry)
    builder.Services.AddSingleton<IHardwareProvider, WmiProvider>();
    builder.Services.AddSingleton<ISystemMetrics, WindowsSystemMetrics>();
    builder.Services.AddSingleton<IInventoryProvider, WindowsInventoryProvider>();
#else
    throw new PlatformNotSupportedException("Este build não contém suporte para Windows.");
#endif
}
else
{
    throw new PlatformNotSupportedException("Plataforma não suportada pelo Sentinel.");
}

// 3. Registro do Gerador de HWID e Cliente WebSocket
builder.Services.AddSingleton<HwidGenerator>();
builder.Services.AddSingleton(sp => 
{
    var hwidGen = sp.GetRequiredService<HwidGenerator>();
    var buffer = sp.GetRequiredService<OfflineBuffer<MetricsPacket>>();
    var inventory = sp.GetRequiredService<IInventoryProvider>();
    
    const string serverUrl = "ws://localhost:5000/agent-hub"; 
    
    return new WssClient(serverUrl, hwidGen, buffer, inventory);
});

// 4. Registro do Worker Service (O Orquestrador)
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();
