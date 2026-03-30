namespace SentinelAgente.Agent.Core.Communication;

/// <summary>
/// Provedor de lógica para cálculo de intervalos de reconexão resilientes.
/// </summary>
public static class BackoffPolicy
{
    /// <summary>
    /// Calcula o tempo de espera exponencial com Jitter para evitar thundering herd.
    /// </summary>
    /// <param name="retryAttempt">O número da tentativa atual (iniciando em 0 ou 1).</param>
    /// <param name="maxDelaySeconds">O teto máximo de espera em segundos.</param>
    /// <returns>Um TimeSpan representando o tempo de espera sugerido.</returns>
    public static TimeSpan CalculateDelay(int retryAttempt, int maxDelaySeconds = 60)
    {
        // Base exponencial: 2 ^ tentativa
        double seconds = Math.Pow(2, retryAttempt);

        // Aplica o teto (Ceiling) para não exceder o limite de 60s (ou o valor informado)
        seconds = Math.Min(seconds, maxDelaySeconds);

        // Jitter aleatório (10ms a 500ms) para dispersar reconexões simultâneas de múltiplos agentes
        int jitterMilliseconds = Random.Shared.Next(10, 501);

        return TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(jitterMilliseconds);
    }
}
