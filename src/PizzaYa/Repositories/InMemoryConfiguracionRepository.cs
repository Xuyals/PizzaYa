using System.Collections.Concurrent;

namespace PizzaYa.Repositories;

public class InMemoryConfiguracionRepository : IConfiguracionRepository
{
    private readonly ConcurrentDictionary<string, string> _valores = new();

    public Task<string?> ObtenerAsync(string clave) =>
        Task.FromResult(_valores.TryGetValue(clave, out var valor) ? valor : null);

    public Task GuardarAsync(string clave, string valor)
    {
        _valores[clave] = valor;
        return Task.CompletedTask;
    }
}
