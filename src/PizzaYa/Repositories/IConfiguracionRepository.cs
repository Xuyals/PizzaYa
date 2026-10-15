namespace PizzaYa.Repositories;

/// <summary>Valores de configuración de la tienda que cambia el administrador (HU7), por ejemplo el valor del domicilio.</summary>
public interface IConfiguracionRepository
{
    /// <summary>Devuelve el valor guardado, o null si esa clave todavía no tiene valor.</summary>
    Task<string?> ObtenerAsync(string clave);

    Task GuardarAsync(string clave, string valor);
}
