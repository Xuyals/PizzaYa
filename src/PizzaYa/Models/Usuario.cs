using System.ComponentModel.DataAnnotations;

namespace PizzaYa.Models;

/// <summary>Roles de la tienda (HU8). Quien se registra siempre queda como Cliente.</summary>
public static class RolUsuario
{
    public const string Cliente = "Cliente";
    public const string Administrador = "Administrador";
    public const string Cocinero = "Cocinero";

    /// <summary>Roles que pueden entrar al panel de cocina y al de inventario.</summary>
    public const string Personal = Administrador + "," + Cocinero;

    public static readonly IReadOnlyList<string> Todos = new[] { Cliente, Administrador, Cocinero };
}

/// <summary>Cuenta de la tienda (HU8). La contraseña nunca se guarda: solo su hash.</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = RolUsuario.Cliente;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class RegistroViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede pasar de 100 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede pasar de 150 caracteres.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Repite la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarPassword { get; set; } = "";
}

public class IngresarViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
