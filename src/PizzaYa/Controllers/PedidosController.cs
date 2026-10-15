using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Controllers;

/// <summary>HU4 – Panel de cocina (Index, Cambiar) y consulta del pedido por el cliente (Seguimiento).</summary>
public class PedidosController : Controller
{
    private readonly IPedidoRepository _pedidos;
    private readonly PedidoService _servicio;

    public PedidosController(IPedidoRepository pedidos, PedidoService servicio)
    {
        _pedidos = pedidos;
        _servicio = servicio;
    }

    // GET: /Pedidos
    /// <summary>Panel de cocina: pedidos del más antiguo al más reciente.</summary>
    [Authorize(Roles = RolUsuario.Personal)]
    public async Task<IActionResult> Index()
    {
        var pedidos = await _pedidos.ObtenerTodosAsync();
        return View(pedidos.OrderBy(p => p.CreadoEn).ThenBy(p => p.Id).ToList());
    }

    // POST: /Pedidos/Cambiar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RolUsuario.Personal)]
    public async Task<IActionResult> Cambiar(int id, string nuevoEstado)
    {
        var resultado = await _servicio.CambiarEstadoAsync(id, nuevoEstado);
        TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    // GET: /Pedidos/Seguimiento?numero=PZ-20261014-A3F7
    /// <summary>El cliente consulta su pedido con el número que le dieron al confirmar.</summary>
    [HttpGet]
    public async Task<IActionResult> Seguimiento(string? numero)
    {
        var modelo = new SeguimientoViewModel { Numero = numero?.Trim() };
        if (!string.IsNullOrWhiteSpace(modelo.Numero))
        {
            modelo.Buscado = true;
            modelo.Pedido = await _pedidos.ObtenerPorNumeroAsync(modelo.Numero);
        }
        return View(modelo);
    }
}
