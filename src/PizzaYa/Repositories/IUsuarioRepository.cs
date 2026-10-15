using PizzaYa.Models;

namespace PizzaYa.Repositories;

public interface IUsuarioRepository
{
    /// <summary>Busca por correo sin distinguir mayúsculas. Devuelve null si no existe.</summary>
    Task<Usuario?> ObtenerPorCorreoAsync(string correo);

    Task<Usuario?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Guarda la cuenta y devuelve el usuario con su id. Devuelve null si el correo ya estaba
    /// registrado (aunque dos personas se registren a la vez, solo una lo logra).
    /// </summary>
    Task<Usuario?> CrearAsync(Usuario usuario);
}
