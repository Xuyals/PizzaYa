using System.Security.Claims;

namespace PizzaYa.Controllers;

public static class ClaimsExtensiones
{
    /// <summary>Id del cliente con sesión iniciada, o null si es un visitante.</summary>
    public static int? UsuarioId(this ClaimsPrincipal usuario) =>
        int.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
