using System.Diagnostics;

namespace SentinelAgente.Agent.Core.Commands;

/// <summary>
/// Despachador de comandos do sistema operacional (Linux Native).
/// </summary>
public static class CommandDispatcher
{
    /// <summary>
    /// Executa uma ação administrativa no Linux host.
    /// </summary>
    /// <param name="action">Ação solicitada (REBOOT, SHUTDOWN, SUSPEND).</param>
    public static void ExecuteCommand(string action)
    {
        string fileName = string.Empty;
        string arguments = string.Empty;

        action = action.ToUpperInvariant();

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

        if (!string.IsNullOrEmpty(fileName))
        {
            try
            {
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
                Console.WriteLine($"[SENTINEL]: Erro ao executar {action}: {ex.Message}");
            }
        }
    }
}
