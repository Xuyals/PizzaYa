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
    private readonly PedidoService  _pedidos;
    private readonly CuponService   _cupones;
    private readonly ConfiguracionService _configuracion;
    private readonly ConsumoService _consumo;

    public CarritoController(CarritoService carrito, PedidoService pedidos, CuponService cupones,
        ConfiguracionService configuracion, ConsumoService consumo)
    {
        _carrito = carrito;
        _pedidos = pedidos;
        _cupones = cupones;
        _configuracion = configuracion;
        _consumo = consumo;
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
            ValorDomicilio = await _configuracion.ObtenerCostoDomicilioAsync(),
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

        // Recalcular domicilio con el valor que configuró el administrador (HU7)
        vm.ValorDomicilio = await _configuracion.ObtenerCostoDomicilioAsync();
        vm.CostoDomicilio = vm.EsDomicilio ? vm.ValorDomicilio : 0m;

        if (!vm.EsDomicilio && vm.MetodoPago != MetodoPagoPedido.AntesDeLaEntrega &&
            vm.MetodoPago != MetodoPagoPedido.AlRecoger)
            ModelState.AddModelError(nameof(vm.MetodoPago), "Escoge una forma de pago válida.");
        if (vm.EsDomicilio && vm.MetodoPago != MetodoPagoPedido.AntesDeLaEntrega)
            ModelState.AddModelError(nameof(vm.MetodoPago), "Los pedidos a domicilio deben pagarse antes de la entrega.");

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

        // ── Inventario (HU9) ───────────────────────────────────────────
        // Se descuentan los ingredientes del pedido; si no alcanzan, no se confirma y se avisa al cliente
        var reserva = await _consumo.DescontarAsync(carrito);
        if (!reserva.Exitoso)
        {
            ModelState.AddModelError("", reserva.Mensaje!);
            return View(vm);
        }

        // ── Pedido confirmado ──────────────────────────────────────────
        // Se guarda en la misma tabla que lee el módulo de pedidos (HU4), en estado "Recibido"
        Pedido pedido;
        try
        {
            pedido = await _pedidos.CrearDesdeCarritoAsync(carrito, new PedidoService.DatosConfirmacion(
                _carrito.GenerarNumeroPedido(), vm.Nombre, vm.Telefono, vm.EsDomicilio, vm.Direccion,
                vm.CostoDomicilio, vm.Descuento, User.UsuarioId(), vm.MetodoPago));
        }
        catch
        {
            await _consumo.ReponerAsync(reserva.Descontado); // el pedido no se guardó: el inventario no debe bajar
            throw;
        }

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
            MetodoPago     = vm.MetodoPago,
            FechaPedido    = DateTime.Now,
        };

        // Vaciamos el carrito tras confirmar y el cupón escogido ya se usó
        _carrito.VaciarCarrito(HttpContext.Session);
        HttpContext.Session.Remove(CuponService.ClaveSesion);

        return View("Confirmado", confirmado);
    }
}
