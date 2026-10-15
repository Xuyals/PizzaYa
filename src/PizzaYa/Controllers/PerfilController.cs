using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Services;

namespace PizzaYa.Controllers;

/// <summary>HU6 – Perfil del cliente registrado: favoritas, último pedido y cupones.</summary>
[Authorize]
public class PerfilController : Controller
{
    private readonly PerfilService _perfil;
    private readonly CuponService _cupones;

    public PerfilController(PerfilService perfil, CuponService cupones)
    {
        _perfil = perfil;
        _cupones = cupones;
    }

    private int UsuarioId => User.UsuarioId()!.Value;

    // GET: /Perfil
    public async Task<IActionResult> Index()
    {
        var modelo = await _perfil.ObtenerPerfilAsync(UsuarioId, User.Identity?.Name ?? "");
        return View(modelo);
    }

    // POST: /Perfil/MarcarFavorita  (desde /Precio/Resumen: guarda la pizza que está armando)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarFavorita()
    {
        var json = HttpContext.Session.GetString(PizzaPersonalizada.ClaveSesion);
        var seleccion = json is null ? null : JsonSerializer.Deserialize<PizzaPersonalizada>(json);
        if (seleccion is null)
        {
            TempData["Error"] = "No hay una pizza configurada para guardar.";
            return RedirectToAction("Index", "Menu");
        }

        var favorita = await _perfil.MarcarFavoritaAsync(UsuarioId, seleccion);
        if (favorita is null)
        {
            TempData["Error"] = "Termina de configurar la pizza (tamaño y borde) antes de marcarla como favorita.";
            return RedirectToAction("Personalizar", "Menu", new { id = seleccion.PizzaId });
        }

        TempData["Exito"] = "⭐ Pizza guardada en tus favoritas.";
        return RedirectToAction("Resumen", "Precio");
    }

    // POST: /Perfil/PedirFavorita/5  (pedirla de nuevo en un clic)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PedirFavorita(int id)
    {
        if (!await _perfil.PedirFavoritaAsync(UsuarioId, id, HttpContext.Session))
        {
            TempData["Error"] = "Esa pizza favorita ya no está disponible en el menú.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] = "✓ Pizza favorita agregada al carrito.";
        return RedirectToAction("Index", "Carrito");
    }

    // POST: /Perfil/EliminarFavorita/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarFavorita(int id, [FromServices] PizzaYa.Repositories.IFavoritaRepository favoritas)
    {
        await favoritas.EliminarAsync(id, UsuarioId);
        TempData["Exito"] = "Pizza quitada de tus favoritas.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Perfil/RepetirUltimo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RepetirUltimo()
    {
        var (agregadas, omitidas) = await _perfil.RepetirUltimoPedidoAsync(UsuarioId, HttpContext.Session);
        if (agregadas == 0)
        {
            TempData["Error"] = "No se pudo repetir el pedido: sus pizzas ya no están en el menú.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] = omitidas == 0
            ? "✓ Tu último pedido está en el carrito."
            : $"✓ Tu último pedido está en el carrito, pero {omitidas} pizza(s) ya no están en el menú.";
        return RedirectToAction("Index", "Carrito");
    }

    // POST: /Perfil/AplicarCupon
    /// <summary>
    /// Deja el cupón listo para el próximo pedido (se aplica al confirmar). Si el cupón no existe o
    /// está vencido, avisa con un error y no cambia nada.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AplicarCupon(string? codigo)
    {
        // El monto no importa aquí: solo se comprueba que el cupón exista y esté vigente
        var (_, aplicado, mensaje) = await _cupones.ResolverAsync(codigo, 1_000_000m, UsuarioId);
        if (!aplicado)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(codigo) ? "Escribe el código del cupón." : mensaje;
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetString(CuponService.ClaveSesion, codigo!.Trim().ToUpperInvariant());
        TempData["Exito"] = $"✓ Cupón {codigo.Trim().ToUpperInvariant()} listo: se descontará al confirmar tu pedido.";
        return RedirectToAction("Confirmar", "Carrito");
    }
}
