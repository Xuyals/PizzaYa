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
    private readonly CuponService   _cupones;

    public CarritoController(CarritoService carrito, PedidoService pedidos, CuponService cupones)
    {
        _carrito = carrito;
        _pedidos = pedidos;
        _cupones = cupones;
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
    public async Task<IActionResult> Confirmar()
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
            // Con sesión iniciada se adelanta el nombre
            Nombre = User.Identity?.IsAuthenticated == true ? User.Identity.Name ?? "" : "",
            // Cupón que el cliente escogió en su perfil (HU6)
            CodigoCupon = HttpContext.Session.GetString(CuponService.ClaveSesion),
        };

        // Si venía un cupón escogido, se muestra ya el descuento
        if (!string.IsNullOrWhiteSpace(vm.CodigoCupon))
        {
            var (descuento, aplicado, mensaje) = await _cupones.ResolverAsync(vm.CodigoCupon, vm.Subtotal, User.UsuarioId());
            vm.Descuento = descuento;
            if (!aplicado)
            {
                ModelState.AddModelError(nameof(vm.CodigoCupon), mensaje ?? "Cupón no válido o expirado.");
                HttpContext.Session.Remove(CuponService.ClaveSesion);
            }
        }

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

        // Aplicar cupón si viene. Un cupón que no existe o está vencido (HU6) no cambia el total:
        // se avisa con el error y el pedido no se confirma hasta corregirlo o dejar el campo vacío.
        if (!string.IsNullOrWhiteSpace(vm.CodigoCupon))
        {
            var (descuento, aplicado, mensaje) = await _cupones.ResolverAsync(
                vm.CodigoCupon, vm.Subtotal + vm.CostoDomicilio, User.UsuarioId());
            vm.Descuento = descuento;
            if (!aplicado)
                ModelState.AddModelError(nameof(vm.CodigoCupon), mensaje ?? "Cupón no válido o expirado.");
        }

        if (!ModelState.IsValid)
            return View(vm);

        // ── Pedido confirmado ──────────────────────────────────────────
        // Se guarda en la misma tabla que lee el módulo de pedidos (HU4), en estado "Recibido"
        var pedido = await _pedidos.CrearDesdeCarritoAsync(carrito, new PedidoService.DatosConfirmacion(
            _carrito.GenerarNumeroPedido(), vm.Nombre, vm.Telefono, vm.EsDomicilio, vm.Direccion,
            vm.CostoDomicilio, vm.Descuento, User.UsuarioId()));

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

        // Vaciamos el carrito tras confirmar y el cupón escogido ya se usó
        _carrito.VaciarCarrito(HttpContext.Session);
        HttpContext.Session.Remove(CuponService.ClaveSesion);

        return View("Confirmado", confirmado);
    }
}
