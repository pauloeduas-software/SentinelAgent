using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SentinelAgente.Agent.Linux.Security;

/// <summary>
/// Provedor de Segurança Ofensiva para camuflagem e proteção de integridade no Linux.
/// </summary>
public static class LinuxSecurity
{
    private const int PR_SET_NAME = 15;

    [DllImport("libc", SetLastError = true)]
    private static extern int prctl(int option, string arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

    /// <summary>
    /// Mascara o nome do processo no Kernel (visível no htop/ps).
    /// Nota: O Kernel Linux limita o nome para 16 caracteres.
    /// </summary>
    public static void CamouflageProcess(string fakeName)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // PR_SET_NAME mudará o nome que aparece no htop/comm
                prctl(PR_SET_NAME, fakeName, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
        }
        catch { /* Falha silenciosa para não impedir o início do serviço */ }
    }

    /// <summary>
    /// Ativa a proteção de imutabilidade (Immutable Bit) no binário do agente.
    /// Impede deleção, renomeação ou escrita, mesmo pelo usuário ROOT.
    /// </summary>
    public static void HardenBinary()
    {
        // REGRA DE SEGURANÇA: Só tranca o binário se estiver em RELEASE (Produção)
#if !DEBUG
        try
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return;

            // Executa o comando de sistema chattr +i
            // Como o agente roda via Systemd como root, ele tem o CAP_LINUX_IMMUTABLE
            Process.Start(new ProcessStartInfo
            {
                FileName = "chattr",
                Arguments = $"+i {exePath}",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();
            
            Console.WriteLine("[SECURITY]: Binário trancado com sucesso (Immutable Bit).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SECURITY_ERROR]: Falha ao aplicar trava de imutabilidade: {ex.Message}");
        }
#else
        Console.WriteLine("[SECURITY]: Modo DEBUG detectado. Trava de imutabilidade IGNORADA para segurança do dev.");
#endif
    }
}
