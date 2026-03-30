# SentinelAgente - O Operário de Inventário

O **SentinelAgente** é um serviço de monitoramento leve e resiliente, desenvolvido em .NET 8, projetado para rodar em segundo plano em máquinas Windows e Linux.

## Funções Principais
- **Identidade Única (HWID):** Geração de ID imutável baseado em hardware (Motherboard, CPU, Disco).
- **Telemetria Nativa:** Coleta de uso de CPU, Memória RAM e Armazenamento via APIs nativas (P/Invoke no Windows e /proc no Linux).
- **Comunicação Resiliente:** Cliente WebSocket com estratégia de Reconexão Exponencial e Buffer Offline para métricas.

## Tecnologias
- .NET 8 (Worker Service)
- WebSockets (Client)
- P/Invoke & Linux Internals

## Como Rodar
1. Certifique-se de ter o SDK do .NET 8 instalado.
2. Configure a URL da API no `Program.cs`.
3. Execute:
   ```bash
   cd src/SentinelAgente.Agent.Worker
   dotnet run
   ```
