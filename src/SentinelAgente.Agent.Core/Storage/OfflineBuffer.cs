using System.Collections.Concurrent;

namespace SentinelAgente.Agent.Core.Storage;

/// <summary>
/// Buffer circular em memória para armazenamento temporário de pacotes durante quedas de rede.
/// </summary>
/// <typeparam name="T">O tipo de pacote a ser armazenado (ex: MetricsPacket).</typeparam>
public class OfflineBuffer<T>(int maxCapacity)
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly int _maxCapacity = maxCapacity;

    /// <summary>
    /// Quantidade atual de itens no buffer.
    /// </summary>
    public int Count => _queue.Count;

    /// <summary>
    /// Adiciona um item à fila. Se a capacidade máxima for atingida, remove o item mais antigo (FIFO).
    /// </summary>
    /// <param name="item">O pacote a ser armazenado.</param>
    public void Enqueue(T item)
    {
        _queue.Enqueue(item);

        // Mantém a fila dentro do limite de capacidade descartando o excedente mais antigo
        while (_queue.Count > _maxCapacity)
        {
            _queue.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Tenta remover e retornar o item mais antigo da fila.
    /// </summary>
    /// <param name="result">O item recuperado, se houver.</param>
    /// <returns>Verdadeiro se um item foi removido; caso contrário, falso.</returns>
    public bool TryDequeue(out T? result) => _queue.TryDequeue(out result);

    /// <summary>
    /// Esvazia o buffer e retorna todos os itens acumulados.
    /// </summary>
    /// <returns>Uma lista contendo todos os itens que estavam no buffer.</returns>
    public List<T> GetAllAndClear()
    {
        var items = new List<T>();
        while (_queue.TryDequeue(out var item))
        {
            items.Add(item);
        }
        return items;
    }
}
