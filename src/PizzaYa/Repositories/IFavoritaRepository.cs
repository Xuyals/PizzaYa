using PizzaYa.Models;

namespace PizzaYa.Repositories;

public interface IFavoritaRepository
{
    /// <summary>Las favoritas del cliente, de la más reciente a la más antigua.</summary>
    Task<List<Favorita>> ObtenerPorUsuarioAsync(int usuarioId);

    Task<Favorita?> ObtenerAsync(int id, int usuarioId);

    /// <summary>Guarda la favorita. Si el cliente ya tiene una idéntica, devuelve esa y no duplica.</summary>
    Task<Favorita> AgregarAsync(Favorita favorita);

    Task<bool> EliminarAsync(int id, int usuarioId);
}
