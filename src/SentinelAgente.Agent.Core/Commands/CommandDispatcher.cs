using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SentinelAgente.Agent.Core.Commands;

/// <summary>
/// Despachador de comandos do sistema operacional.
/// </summary>
public static class CommandDispatcher
{
    /// <summary>
    /// Executa uma ação administrativa no Sistema Operacional host.
    /// </summary>
    /// <param name="action">Ação solicitada (REBOOT, SHUTDOWN, SUSPEND).</param>
    public static void ExecuteCommand(string action)
    {
        string fileName = string.Empty;
        string arguments = string.Empty;

        action = action.ToUpperInvariant();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            switch (action)
            {
                case "REBOOT":
                    fileName = "shutdown";
                    arguments = "/r /t 0";
                    break;
                case "SHUTDOWN":
                    fileName = "shutdown";
                    arguments = "/s /t 0";
                    break;
                case "SUSPEND":
                    fileName = "rundll32.exe";
                    arguments = "powrprof.dll,SetSuspendState 0,1,0";
                    break;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            switch (action)
            {
                case "REBOOT":
                    fileName = "reboot";
                    break;
                case "SHUTDOWN":
                    fileName = "shutdown";
                    arguments = "-h now";
                    break;
                case "SUSPEND":
                    fileName = "systemctl";
                    arguments = "suspend";
                    break;
            }
        }

        if (!string.IsNullOrEmpty(fileName))
        {
            try
            {
                // Inicia o processo de comando de forma silenciosa
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                // Em um cenário real, aqui enviaríamos um AckPacket de erro para o servidor
                Console.WriteLine($"[ERRO]: Falha ao executar comando {action}: {ex.Message}");
            }
        }
    }
}
