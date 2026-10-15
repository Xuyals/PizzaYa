using Microsoft.AspNetCore.Mvc;
using PizzaYa.Repositories;

namespace PizzaYa.Controllers;

/// <summary>Módulo de pedidos (HU4). Los pedidos se crean desde el carrito (HU3).</summary>
public class PedidosController : Controller
{
    private readonly IPedidoRepository _pedidos;

    public PedidosController(IPedidoRepository pedidos)
    {
        _pedidos = pedidos;
    }

    // GET: /Pedidos/Seguimiento?numero=PZ-20261014-A3F7
    [HttpGet]
    public async Task<IActionResult> Seguimiento(string? numero)
    {
        var pedido = string.IsNullOrWhiteSpace(numero) ? null : await _pedidos.ObtenerPorNumeroAsync(numero);
        if (pedido is null) return NotFound();
        return View(pedido);
    }
}
