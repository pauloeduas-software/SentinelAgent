using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SentinelAgente.Agent.Core.Communication;
using SentinelAgente.Agent.Core.Identity;
using SentinelAgente.Agent.Core.Storage;
using SentinelAgente.Agent.Core.Metrics;
using SentinelAgente.Agent.Linux.Security; // Namespace de segurança ofensiva
using SentinelAgente.Shared.Packets;
using SentinelAgente.Agent.Worker;
using System.Diagnostics;

// Namespaces nativos Linux
using SentinelAgente.Agent.Linux.Identity;
using SentinelAgente.Agent.Linux.Hardware;
using SentinelAgente.Agent.Linux.Metrics;

// --- VERIFICAÇÃO DE PRIVILÉGIOS (ROOT ONLY) ---
if (Environment.UserName != "root")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n[ERRO CRÍTICO]: O Sentinel Agent requer privilégios de ROOT para operar.");
    Console.WriteLine("Por favor, execute com 'sudo dotnet run' ou instale como serviço.");
    Console.ForegroundColor = ConsoleColor.Gray;
    return;
}
// ----------------------------------------------

// --- FASE DE AUTO-INSTALAÇÃO (SYSTEMD) ---
if (args.Contains("--install"))
{
    try
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) throw new Exception("Não foi possível localizar o caminho do executável.");

        string serviceContent = $"""
        [Unit]
        Description=Sentinel Remote Agent
        After=network.target

        [Service]
        ExecStart={exePath}
        WorkingDirectory={Path.GetDirectoryName(exePath)}
        Restart=always
        User=root

        [Install]
        WantedBy=multi-user.target
        """;

        const string servicePath = "/etc/systemd/system/sentinel-agent.service";
        
        Console.WriteLine($"[INSTALLER]: Gravando serviço em {servicePath}...");
        File.WriteAllText(servicePath, serviceContent);

        Console.WriteLine("[INSTALLER]: Atualizando Daemon-Reload...");
        Process.Start("systemctl", "daemon-reload").WaitForExit();

        Console.WriteLine("[INSTALLER]: Ativando serviço (Enable)...");
        Process.Start("systemctl", "enable sentinel-agent.service").WaitForExit();

        Console.WriteLine("[INSTALLER]: Iniciando serviço (Start)...");
        Process.Start("systemctl", "start sentinel-agent.service").WaitForExit();

        Console.WriteLine("\n[SUCCESS]: Sentinel Remote Agent instalado e rodando como root via Systemd!");
        return;
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("\n[ERROR]: Permissão negada! Você precisa executar com 'sudo' para instalar o serviço.");
        return;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[ERROR]: Falha crítica na instalação: {ex.Message}");
        return;
    }
}
// -----------------------------------------

// --- FASE DE SEGURANÇA E CAMUFLAGEM ---
// Mimetiza um serviço nativo do sistema para observadores casuais
LinuxSecurity.CamouflageProcess("systemd-network-audit");

// Tranca o arquivo binário em tempo de execução (Somente em RELEASE)
LinuxSecurity.HardenBinary();
// --------------------------------------

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
