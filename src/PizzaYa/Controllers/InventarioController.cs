using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Controllers;

/// <summary>HU5 (Equipo 4): menú de inventario y pedidos a proveedores.</summary>
[Authorize(Roles = RolUsuario.Personal)]
public class InventarioController : Controller
{
    private readonly IInventarioRepository _inventario;
    private readonly InventarioService _servicio;

    public InventarioController(IInventarioRepository inventario, InventarioService servicio)
    {
        _inventario = inventario;
        _servicio = servicio;
    }

    // GET: /Inventario
    public async Task<IActionResult> Index()
    {
        var modelo = new InventarioViewModel
        {
            Ingredientes = await _inventario.ObtenerInventarioAsync(),
            Pedidos = await _inventario.ObtenerPedidosAsync(),
        };
        return View(modelo);
    }

    // GET: /Inventario/NuevoPedido                    -> formulario vacío
    // GET: /Inventario/NuevoPedido?ingredienteId=5    -> botón "Pedir" de una fila por agotarse
    // GET: /Inventario/NuevoPedido?porAgotarse=true   -> pedir todos los que están por agotarse
    public async Task<IActionResult> NuevoPedido(int? ingredienteId, bool porAgotarse = false)
    {
        var ingredientes = await _inventario.ObtenerInventarioAsync();
        var pedido = new NuevoPedidoProveedor
        {
            Lineas = ingredientes.Select(i => new LineaPedido
            {
                IngredienteId = i.Id,
                Cantidad = (porAgotarse && i.PorAgotarse) || i.Id == ingredienteId
                    ? (decimal?)_servicio.SugerirCantidad(i)
                    : null,
            }).ToList(),
        };
        return View(new NuevoPedidoViewModel { Ingredientes = ingredientes, Pedido = pedido });
    }

    // POST: /Inventario/NuevoPedido
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoPedido([Bind(Prefix = "Pedido")] NuevoPedidoProveedor pedido)
    {
        var ingredientes = await _inventario.ObtenerInventarioAsync();

        var errores = _servicio.Validar(pedido, ingredientes);
        foreach (var (campo, mensaje) in errores)
            ModelState.AddModelError($"Pedido.{campo}", mensaje);

        if (errores.Count > 0 || !ModelState.IsValid)
            return View(new NuevoPedidoViewModel { Ingredientes = ingredientes, Pedido = pedido });

        await _inventario.CrearPedidoAsync(pedido.Proveedor!.Trim(), _servicio.LineasAPedir(pedido));
        TempData["Mensaje"] = "Pedido guardado como Pendiente.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Inventario/Recibir/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recibir(int id)
    {
        if (await _inventario.RecibirPedidoAsync(id))
            TempData["Mensaje"] = "Pedido recibido: las cantidades ya se sumaron al inventario.";
        else
            TempData["Error"] = "Ese pedido no existe o ya fue recibido.";

        return RedirectToAction(nameof(Index));
    }
}
