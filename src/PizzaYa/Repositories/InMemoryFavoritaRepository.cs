using PizzaYa.Models;

namespace PizzaYa.Repositories;

public class InMemoryFavoritaRepository : IFavoritaRepository
{
    private readonly List<Favorita> _favoritas = new();
    private readonly object _candado = new();
    private int _siguienteId = 1;

    public Task<List<Favorita>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        lock (_candado)
            return Task.FromResult(_favoritas.Where(f => f.UsuarioId == usuarioId)
                .OrderByDescending(f => f.CreadoEn).ThenByDescending(f => f.Id).ToList());
    }

    public Task<Favorita?> ObtenerAsync(int id, int usuarioId)
    {
        lock (_candado)
            return Task.FromResult(_favoritas.FirstOrDefault(f => f.Id == id && f.UsuarioId == usuarioId));
    }

    public Task<Favorita> AgregarAsync(Favorita favorita)
    {
        lock (_candado)
        {
            var igual = _favoritas.FirstOrDefault(f => f.UsuarioId == favorita.UsuarioId && EsIgual(f, favorita));
            if (igual is not null) return Task.FromResult(igual);

            favorita.Id = _siguienteId++;
            _favoritas.Add(favorita);
            return Task.FromResult(favorita);
        }
    }

    public Task<bool> EliminarAsync(int id, int usuarioId)
    {
        lock (_candado)
            return Task.FromResult(_favoritas.RemoveAll(f => f.Id == id && f.UsuarioId == usuarioId) > 0);
    }

    private static bool EsIgual(Favorita a, Favorita b) =>
        a.PizzaId == b.PizzaId && a.TamanoId == b.TamanoId && a.BordeId == b.BordeId
        && a.ExtrasIds.OrderBy(x => x).SequenceEqual(b.ExtrasIds.OrderBy(x => x))
        && a.QuitadosIds.OrderBy(x => x).SequenceEqual(b.QuitadosIds.OrderBy(x => x));
}
