namespace SentinelAgente.Agent.Core.Identity;

/// <summary>
/// Define o contrato para provedores de hardware específicos de cada sistema operacional.
/// </summary>
public interface IHardwareProvider
{
    /// <summary>
    /// Recupera o UUID ou Serial da Placa-mãe.
    /// </summary>
    string? GetMotherboardId();

    /// <summary>
    /// Recupera o identificador único do Processador.
    /// </summary>
    string? GetCpuId();

    /// <summary>
    /// Recupera o número de série do disco de boot/principal.
    /// </summary>
    string? GetDiskSerialNumber();
}
