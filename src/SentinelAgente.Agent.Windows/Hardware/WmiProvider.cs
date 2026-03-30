using System.Management;
using SentinelAgente.Agent.Core.Identity;

namespace SentinelAgente.Agent.Windows.Hardware;

/// <summary>
/// Provedor de hardware específico para Windows, utilizando WMI (Windows Management Instrumentation).
/// </summary>
public class WmiProvider : IHardwareProvider
{
    /// <summary>
    /// Recupera o Serial Number da Placa-mãe via Win32_BaseBoard.
    /// </summary>
    public string? GetMotherboardId() => 
        GetWmiValue("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber");

    /// <summary>
    /// Recupera o ProcessorId único via Win32_Processor.
    /// </summary>
    public string? GetCpuId() => 
        GetWmiValue("SELECT ProcessorId FROM Win32_Processor", "ProcessorId");

    /// <summary>
    /// Recupera o Serial Number do disco físico principal (Índice 0).
    /// </summary>
    public string? GetDiskSerialNumber() => 
        GetWmiValue("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index = 0", "SerialNumber");

    /// <summary>
    /// Método auxiliar para execução de consultas WMI com tratamento de erro resiliente.
    /// </summary>
    /// <param name="query">A query WQL a ser executada.</param>
    /// <param name="property">O nome da propriedade a ser extraída.</param>
    /// <returns>O valor da propriedade ou null em caso de erro no WMI.</returns>
    private static string? GetWmiValue(string query, string property)
    {
        try
        {
            // ManagementObjectSearcher implementa IDisposable e gerencia handles do COM internamente.
            using var searcher = new ManagementObjectSearcher(query);
            
            // Get() retorna uma coleção que também precisa ser descartada.
            using var collection = searcher.Get();

            foreach (var obj in collection)
            {
                // Extrai o valor, remove espaços e valida se não é vazio.
                var value = obj[property]?.ToString()?.Trim();
                
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch (ManagementException)
        {
            // Erros específicos de permissão ou repositório WMI corrompido.
            // Retornamos null para que o HwidGenerator (Core) tente o quórum com outras peças.
        }
        catch (Exception)
        {
            // Outros erros genéricos de sistema.
        }

        return null;
    }
}
