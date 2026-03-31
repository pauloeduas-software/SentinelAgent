# SentinelAgente - O Operário de Inventário

O **SentinelAgente** é um serviço de monitoramento e gestão remota (RMM) leve e resiliente, desenvolvido em .NET 8.

## Novas Funcionalidades (v2.0)
- **Gestão Ativa (Command Dispatcher):** Agora o agente escuta comandos via WebSocket e executa ações de energia (Reboot, Shutdown, Suspend) diretamente no SO.
- **Inventário Profundo (ITAM):** Coleta de MAC Address, IP Local, Modelo de CPU e lista completa de Softwares instalados (Windows Registry).
- **Telemetria de Disco Corrigida:** Reporte preciso em Gigabytes (Total vs Usado) com tratamento de permissões no Linux.

## Tecnologias
- .NET 8 (Worker Service)
- WebSockets Bidirecional
- P/Invoke (Win32) & Procfs/Sysfs (Linux)

## Como Rodar
1. SDK .NET 8 instalado.
2. `cd src/SentinelAgente.Agent.Worker`
3. `dotnet run`
