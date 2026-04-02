namespace SentinelAgente.Agent.Core.Identity;

/// <summary>
/// Contrato para extração de identificadores puramente físicos de hardware.
/// </summary>
public interface IHardwareProvider
{
    /// <summary>
    /// Recupera o Serial do Produto ou da Placa-mãe (DMI/BIOS).
    /// </summary>
    string? GetMotherboardId();

    /// <summary>
    /// Recupera o modelo/ID do processador.
    /// </summary>
    string? GetCpuId();
}
