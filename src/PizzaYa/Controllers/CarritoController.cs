using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Services;

namespace PizzaYa.Controllers;

/// <summary>
/// HU3 – Gestiona el carrito de compras y la confirmación del pedido.
/// </summary>
public class CarritoController : Controller
{
    private readonly CarritoService _carrito;
    private readonly PrecioService  _precio;
    private readonly PedidoService  _pedidos;

    public CarritoController(CarritoService carrito, PrecioService precio, PedidoService pedidos)
    {
        _carrito = carrito;
        _precio  = precio;
        _pedidos = pedidos;
    }

    // GET: /Carrito
    public IActionResult Index()
    {
        var carrito = _carrito.ObtenerCarrito(HttpContext.Session);
        var vm = new CarritoViewModel
        {
            Carrito = carrito,
            EsDomicilio = false,
            CostoDomicilio = 0m,
        };

        if (TempData["Exito"] is string msg)
            ViewBag.Exito = msg;

        return View(vm);
    }

    // POST: /Carrito/Eliminar/{itemId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(Guid itemId)
    {
        _carrito.EliminarItem(HttpContext.Session, itemId);
        TempData["Exito"] = "Ítem eliminado del carrito.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Carrito/CambiarCantidad
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarCantidad(Guid itemId, int cantidad)
    {
        if (cantidad < 0) cantidad = 0;
        _carrito.CambiarCantidad(HttpContext.Session, itemId, cantidad);
        return RedirectToAction(nameof(Index));
    }

    // GET: /Carrito/Confirmar
    public IActionResult Confirmar()
    {
        var carrito = _carrito.ObtenerCarrito(HttpContext.Session);

        // HU3 – No permitir confirmar si el carrito está vacío
        if (carrito.EstaVacio)
        {
            TempData["Error"] = "Tu carrito está vacío. Agrega pizzas antes de confirmar.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new ConfirmarPedidoViewModel
        {
            Carrito = carrito,
            EsDomicilio = false,
        };

        return View(vm);
    }

    // POST: /Carrito/Confirmar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(ConfirmarPedidoViewModel vm)
    {
        // Recargamos el carrito desde sesión (no llega en el POST)
        var carrito = _carrito.ObtenerCarrito(HttpContext.Session);

        if (carrito.EstaVacio)
        {
            TempData["Error"] = "Tu carrito está vacío.";
            return RedirectToAction(nameof(Index));
        }

        vm.Carrito = carrito;

        // Recalcular domicilio
        vm.CostoDomicilio = vm.EsDomicilio ? PrecioService.CostoDomicilioFijo : 0m;

        // Aplicar cupón si viene
        if (!string.IsNullOrWhiteSpace(vm.CodigoCupon))
        {
            var (descuento, aplicado, _) = _precio.ResolverCupon(
                vm.CodigoCupon, vm.Subtotal + vm.CostoDomicilio);
            vm.Descuento = descuento;
        }

        if (!ModelState.IsValid)
            return View(vm);

        // ── Pedido confirmado ──────────────────────────────────────────
        // Se guarda en la misma tabla que lee el módulo de pedidos (HU4), en estado "Recibido"
        var pedido = await _pedidos.CrearDesdeCarritoAsync(carrito, new PedidoService.DatosConfirmacion(
            _carrito.GenerarNumeroPedido(), vm.Nombre, vm.Telefono, vm.EsDomicilio, vm.Direccion,
            vm.CostoDomicilio, vm.Descuento));

        var confirmado = new PedidoConfirmadoViewModel
        {
            NumeroPedido   = pedido.NumeroPedido,
            Nombre         = vm.Nombre,
            Telefono       = vm.Telefono,
            Direccion      = vm.EsDomicilio ? vm.Direccion : null,
            EsDomicilio    = vm.EsDomicilio,
            Items          = carrito.Items.ToList(),
            Subtotal       = vm.Subtotal,
            CostoDomicilio = vm.CostoDomicilio,
            Descuento      = vm.Descuento,
            Total          = vm.TotalConDescuento,
            FechaPedido    = DateTime.Now,
        };

        // Vaciamos el carrito tras confirmar
        _carrito.VaciarCarrito(HttpContext.Session);

        return View("Confirmado", confirmado);
    }
}
