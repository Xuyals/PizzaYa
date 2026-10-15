using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Services;

namespace PizzaYa.Controllers;

/// <summary>HU8 – Registro, inicio y cierre de sesión.</summary>
public class CuentaController : Controller
{
    private readonly CuentaService _cuentas;

    public CuentaController(CuentaService cuentas)
    {
        _cuentas = cuentas;
    }

    // GET: /Cuenta/Registrar
    [HttpGet]
    public IActionResult Registrar() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Menu") : View(new RegistroViewModel());

    // POST: /Cuenta/Registrar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var resultado = await _cuentas.RegistrarAsync(modelo.Nombre, modelo.Correo, modelo.Password);
        if (!resultado.Exitoso)
        {
            ModelState.AddModelError(resultado.Campo ?? "", resultado.Error ?? "No se pudo crear la cuenta.");
            return View(modelo);
        }

        // La cuenta queda creada y el cliente entra a la tienda
        await IniciarSesionAsync(resultado.Usuario!);
        return RedirectToAction("Index", "Menu");
    }

    // GET: /Cuenta/Ingresar
    [HttpGet]
    public IActionResult Ingresar(string? returnUrl = null) =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Menu")
            : View(new IngresarViewModel { ReturnUrl = returnUrl });

    // POST: /Cuenta/Ingresar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(IngresarViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var usuario = await _cuentas.ValidarCredencialesAsync(modelo.Correo, modelo.Password);
        if (usuario is null)
        {
            // Mismo mensaje si falla el correo o la contraseña: no se revela cuál de los dos
            ModelState.AddModelError("", "El correo o la contraseña no son correctos.");
            modelo.Password = "";
            return View(modelo);
        }

        await IniciarSesionAsync(usuario);

        if (!string.IsNullOrEmpty(modelo.ReturnUrl) && Url.IsLocalUrl(modelo.ReturnUrl))
            return LocalRedirect(modelo.ReturnUrl);
        return RedirectSegunRol(usuario.Rol);
    }

    // POST: /Cuenta/Salir
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Menu");
    }

    // GET: /Cuenta/AccesoDenegado
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    /// <summary>Cada rol entra a lo suyo: el cliente a la tienda, el administrador al inventario y el cocinero a la cocina.</summary>
    private IActionResult RedirectSegunRol(string rol) => rol switch
    {
        RolUsuario.Administrador => RedirectToAction("Index", "Inventario"),
        RolUsuario.Cocinero => RedirectToAction("Index", "Pedidos"),
        _ => RedirectToAction("Index", "Menu"),
    };

    private async Task IniciarSesionAsync(Usuario usuario)
    {
        var identidad = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Role, usuario.Rol),
        }, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = false });
    }
}
