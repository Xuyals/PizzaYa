using Microsoft.AspNetCore.Identity;
using PizzaYa.Models;
using PizzaYa.Repositories;

namespace PizzaYa.Services;

/// <summary>HU8 – Registro e inicio de sesión. Las contraseñas se guardan con hash (ASP.NET Core Identity).</summary>
public class CuentaService
{
    public const int MinimoPassword = 8;

    private readonly IUsuarioRepository _usuarios;
    private readonly PasswordHasher<Usuario> _hasher = new();
    private readonly string _hashFalso;

    public CuentaService(IUsuarioRepository usuarios)
    {
        _usuarios = usuarios;
        // Se verifica contra este hash cuando el correo no existe, para que ingresar con un correo
        // inexistente no responda notablemente más rápido que con uno registrado.
        _hashFalso = _hasher.HashPassword(new Usuario(), Guid.NewGuid().ToString("N"));
    }

    /// <summary>Resultado del registro: <see cref="Usuario"/> si se creó; si no, el campo y el mensaje del error.</summary>
    public record ResultadoRegistro(Usuario? Usuario, string? Campo = null, string? Error = null)
    {
        public bool Exitoso => Usuario is not null;
    }

    /// <summary>Error de contraseña débil, o null si cumple: mínimo 8 caracteres con letras y números.</summary>
    public static string? ValidarPassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinimoPassword)
            return $"La contraseña debe tener al menos {MinimoPassword} caracteres.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "La contraseña debe tener letras y números.";
        return null;
    }

    /// <summary>Crea una cuenta de cliente (o del rol indicado, solo para el administrador inicial).</summary>
    public async Task<ResultadoRegistro> RegistrarAsync(
        string nombre, string correo, string password, string rol = RolUsuario.Cliente)
    {
        correo = (correo ?? "").Trim();
        var errorPassword = ValidarPassword(password);
        if (errorPassword is not null)
            return new ResultadoRegistro(null, nameof(RegistroViewModel.Password), errorPassword);

        var usuario = new Usuario
        {
            Nombre = (nombre ?? "").Trim(),
            Correo = correo,
            Rol = rol,
            CreadoEn = DateTime.UtcNow,
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, password);

        var creado = await _usuarios.CrearAsync(usuario);
        return creado is null
            ? new ResultadoRegistro(null, nameof(RegistroViewModel.Correo), "Ese correo ya está registrado.")
            : new ResultadoRegistro(creado);
    }

    /// <summary>Devuelve el usuario si el correo y la contraseña coinciden; si no, null.</summary>
    public async Task<Usuario?> ValidarCredencialesAsync(string correo, string password)
    {
        var usuario = await _usuarios.ObtenerPorCorreoAsync(correo ?? "");
        if (usuario is null)
        {
            _hasher.VerifyHashedPassword(new Usuario(), _hashFalso, password ?? "");
            return null;
        }

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password ?? "");
        return resultado == PasswordVerificationResult.Failed ? null : usuario;
    }

    /// <summary>
    /// Crea el administrador inicial si todavía no existe (se llama al arrancar con la sección
    /// "Admin" de la configuración). Devuelve true si lo creó.
    /// </summary>
    public async Task<bool> AsegurarAdministradorAsync(string nombre, string correo, string password)
    {
        if (await _usuarios.ObtenerPorCorreoAsync(correo) is not null) return false;
        var resultado = await RegistrarAsync(nombre, correo, password, RolUsuario.Administrador);
        return resultado.Exitoso;
    }
}
