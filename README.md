# Sentinel Agent 🛡️
> **Core RMM & Telemetry Provider for Linux Systems**

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Linux Native](https://img.shields.io/badge/Platform-Linux-FCC624?logo=linux)
![Security](https://img.shields.io/badge/Security-Root--Only-red)

O **Sentinel Agent** é o componente de baixo nível do ecossistema, responsável pela extração de dados brutos do Kernel Linux e execução de comandos administrativos. Projetado para máxima performance e mínima pegada de memória.

## 📁 Estrutura do Projeto e Responsabilidade dos Arquivos

### `src/SentinelAgente.Agent.Worker/`
*   **`Program.cs`**: Ponto de entrada. Gerencia a trava de privilégios de Root, orquestra a Injeção de Dependência (DI) e configura a auto-instalação no Systemd.
*   **`AgentWorker.cs`**: O orquestrador principal. Mantém o loop de execução infinita que dispara a coleta de métricas.

### `src/SentinelAgente.Agent.Core/`
*   **`Identity/HwidGenerator.cs`**: Lógica de identidade única via Serial DMI/BIOS.
*   **`Communication/WssClient.cs`**: Cliente WebSocket resiliente com buffer offline.
*   **`Commands/CommandDispatcher.cs`**: Executor de comandos nativos do Linux.

### `src/SentinelAgente.Agent.Linux/`
*   **`Metrics/LinuxSystemMetrics.cs`**: Coleta de CPU, RAM, Discos e Rede (Gb/Kbps).
*   **`Security/LinuxSecurity.cs`**: Implementa o Modo Stealth e Imutabilidade do Binário.
*   **`Hardware/ProcHardwareProvider.cs`**: Leitura direta de hardware via `/sys` e `/proc`.

### `src/SentinelAgente.Shared/`
*   **`Packets/`**: Contratos de dados imutáveis (JSON models) para comunicação com a API.
*   **`Enums/`**: Definições globais de status e estados do sistema.

## 🛠️ Instalação e Configuração

### Instalação como Serviço (Systemd)
Para instalar o agente permanentemente e iniciar com o boot:
```bash
sudo dotnet run --project src/SentinelAgente.Agent.Worker/SentinelAgente.Agent.Worker.csproj --install
```

### Gerenciamento do Serviço
*   **Pausar:** `sudo systemctl stop sentinel-agent.service`
*   **Remover:** `sudo rm /etc/systemd/system/sentinel-agent.service && sudo systemctl daemon-reload`

### Execução em Tempo Real (Debug)
Para rodar manualmente e ver os logs no terminal:
```bash
sudo dotnet run --project src/SentinelAgente.Agent.Worker/SentinelAgente.Agent.Worker.csproj
```
*   **Parar:** Pressione `Ctrl + C` ou use `sudo pkill -f SentinelAgente.Agent.Worker`.

## 🔒 Regras de Operação
1.  **Strict Root:** O agente encerra imediatamente se executado sem sudo.
2.  **DMI Identity:** O HWID é gerado estritamente via hardware físico.
3.  **Grouping:** Processos são agregados por nome para maior clareza.
