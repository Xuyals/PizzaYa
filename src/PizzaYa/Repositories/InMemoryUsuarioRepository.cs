using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Usuarios en memoria: pruebas y modo local (sin Supabase). Se pierden al reiniciar.</summary>
public class InMemoryUsuarioRepository : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = new();
    private readonly object _candado = new();
    private int _siguienteId = 1;

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo)
    {
        lock (_candado)
            return Task.FromResult(_usuarios.FirstOrDefault(u =>
                string.Equals(u.Correo, correo?.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        lock (_candado)
            return Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));
    }

    public Task<Usuario?> CrearAsync(Usuario usuario)
    {
        lock (_candado)
        {
            if (_usuarios.Any(u => string.Equals(u.Correo, usuario.Correo, StringComparison.OrdinalIgnoreCase)))
                return Task.FromResult<Usuario?>(null);
            usuario.Id = _siguienteId++;
            _usuarios.Add(usuario);
            return Task.FromResult<Usuario?>(usuario);
        }
    }
}
