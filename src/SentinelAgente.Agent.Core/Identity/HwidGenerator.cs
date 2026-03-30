using System.Security.Cryptography;
using System.Text;

namespace SentinelAgente.Agent.Core.Identity;

/// <summary>
/// Gera um identificador único de hardware (HWID) resiliente a falhas parciais.
/// </summary>
public class HwidGenerator(IHardwareProvider hardwareProvider)
{
    private readonly IHardwareProvider _hardwareProvider = hardwareProvider;

    /// <summary>
    /// Gera o HWID baseado em um quórum de componentes físicos.
    /// </summary>
    /// <returns>Hash SHA256 de 64 caracteres em hexadecimal.</returns>
    /// <exception cref="InvalidOperationException">Lançada se o quórum mínimo de 2 IDs não for atingido.</exception>
    public string Generate()
    {
        var validIds = new List<string>();

        // Tenta coletar os 3 pilares de identidade física
        TryCollect(validIds, _hardwareProvider.GetMotherboardId);
        TryCollect(validIds, _hardwareProvider.GetCpuId);
        TryCollect(validIds, _hardwareProvider.GetDiskSerialNumber);

        // Regra de Quórum: Precisamos de pelo menos 2 componentes para garantir a imutabilidade
        if (validIds.Count < 2)
        {
            throw new InvalidOperationException(
                $"Falha crítica de integridade de hardware. Quórum insuficiente para gerar HWID (Encontrados: {validIds.Count}/2).");
        }

        // Normalização: Ordenação previsível, sem espaços, tudo em minúsculo
        var normalizedInput = string.Concat(validIds.OrderBy(x => x))
            .Replace(" ", "")
            .ToLowerInvariant();

        return ComputeSha256Hash(normalizedInput);
    }

    private static void TryCollect(List<string> list, Func<string?> collector)
    {
        try
        {
            var result = collector();
            if (!string.IsNullOrWhiteSpace(result))
            {
                list.Add(result.Trim());
            }
        }
        catch
        {
            // Silenciosamente ignora falhas de componentes individuais para manter a resiliência
            // Em um cenário real, poderíamos logar essa falha como um aviso técnico
        }
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
