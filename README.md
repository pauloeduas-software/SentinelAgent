# SentinelAgente - O Operário de Inventário (Sentinel v2.0)

O **SentinelAgente** é a espinha dorsal de coleta do ecossistema Sentinel. Desenvolvido como um **Worker Service** de alta performance em .NET 8, ele é projetado para operar de forma invisível no sistema operacional, garantindo a coleta ininterrupta de telemetria e a execução imediata de comandos remotos.

## 🚀 Funcionalidades Principais

### 1. Monitoramento de Performance (Real-time)
- **CPU**: Coleta de uso global com média aritmética para precisão.
- **RAM**: Leitura instantânea via P/Invoke (Windows) e parse direto de `/proc/meminfo` (Linux).
- **Armazenamento**: Detalhamento em Gigabytes (Total/Usado) de todas as partições físicas, com lógica resiliente a permissões de sistema.

### 2. Inventário Profundo (ITAM)
- **Identidade imutável (HWID)**: Geração de hash SHA256 baseado em um quórum de hardware (Motherboard, CPU, Disk).
- **Software**: Varredura recursiva do Registro do Windows para listar programas instalados.
- **Rede**: Descoberta de IP Local e MAC Address real, filtrando interfaces virtuais.

### 3. Gestão Remota Ativa (RMM)
- **Command Dispatcher**: Motor de execução que recebe instruções via WebSocket para realizar `Reboot`, `Shutdown` ou `Suspend` de forma nativa no SO.

### 4. Resiliência de Rede
- **Exponential Backoff**: Algoritmo de reconexão inteligente com Jitter para evitar sobrecarga no servidor.
- **Offline Buffer**: Fila FIFO circular em RAM que armazena métricas durante quedas de internet.

## 🛠️ Stack Tecnológica
- **Linguagem**: C# 12
- **Framework**: .NET 8 (Long-Term Support)
- **Comunicação**: WebSockets (System.Net.WebSockets)
- **Internals**: P/Invoke (Win32 API), WMI, Procfs, Sysfs.

## 📁 Estrutura do Projeto
- `SentinelAgente.Agent.Core`: Lógica de negócio, resiliência e interfaces.
- `SentinelAgente.Agent.Windows`: Implementações específicas para Windows.
- `SentinelAgente.Agent.Linux`: Implementações nativas para sistemas Unix.
- `SentinelAgente.Agent.Worker`: O host executável e container de Injeção de Dependência.
- `SentinelAgente.Shared`: Contratos e DTOs de comunicação.

## ⚙️ Como Rodar
1. **Pré-requisitos**: SDK do .NET 8.0 instalado.
2. **Configuração**: Ajuste a URL do servidor no `Program.cs` do Worker.
3. **Execução**:
   ```bash
   cd src/SentinelAgente.Agent.Worker
   dotnet run
   ```

## ⚠️ Notas de Build (Cross-Platform)
O projeto utiliza **Compilação Condicional**. Ao compilar em ambientes Linux, as dependências do Windows são automaticamente ignoradas pelo compilador para garantir a integridade do binário.
