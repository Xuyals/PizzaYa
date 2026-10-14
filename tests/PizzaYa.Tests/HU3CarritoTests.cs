using Microsoft.AspNetCore.Http;
using PizzaYa.Models;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>
/// Criterios de aceptación de la HU3 – Carrito y confirmación de pedido.
/// </summary>
public class HU3CarritoTests
{
    // ── Helpers ─────────────────────────────────────────────────────────

    private static CarritoService CrearServicio() => new();

    /// <summary>
    /// Simula una sesión en memoria usando un diccionario.
    /// Evita dependencia de IHttpContextAccessor en pruebas unitarias.
    /// </summary>
    private static ISession CrearSesion() => new FakeSession();

    private static ItemCarrito ItemEjemplo(string nombre = "Hawaiana", decimal precio = 28_000m) =>
        new()
        {
            NombrePizza    = nombre,
            NombreTamano   = "Personal",
            NombreBorde    = "Tradicional",
            PrecioUnitario = precio,
            Cantidad       = 1,
        };

    // ── Criterio: carrito vacío al inicio ───────────────────────────────

    [Fact]
    public void CarritoNuevo_EstaVacio()
    {
        var svc     = CrearServicio();
        var sesion  = CrearSesion();
        var carrito = svc.ObtenerCarrito(sesion);
        Assert.True(carrito.EstaVacio);
    }

    // ── Criterio: agregar pizza al carrito ──────────────────────────────

    [Fact]
    public void AgregarItem_IncrementaConteo()
    {
        var svc    = CrearServicio();
        var sesion = CrearSesion();

        svc.AgregarItem(sesion, ItemEjemplo());

        var carrito = svc.ObtenerCarrito(sesion);
        Assert.False(carrito.EstaVacio);
        Assert.Single(carrito.Items);
    }

    [Fact]
    public void AgregarVariosItems_TodosQuedan()
    {
        var svc    = CrearServicio();
        var sesion = CrearSesion();

        svc.AgregarItem(sesion, ItemEjemplo("Hawaiana"));
        svc.AgregarItem(sesion, ItemEjemplo("Mexicana"));
        svc.AgregarItem(sesion, ItemEjemplo("Pepperoni"));

        Assert.Equal(3, svc.ObtenerCarrito(sesion).Items.Count);
    }

    // ── Criterio: subtotal correcto ─────────────────────────────────────

    [Fact]
    public void Subtotal_EsSumaDeTodosLosItems()
    {
        var svc    = CrearServicio();
        var sesion = CrearSesion();

        svc.AgregarItem(sesion, new ItemCarrito { PrecioUnitario = 28_000m, Cantidad = 1 });
        svc.AgregarItem(sesion, new ItemCarrito { PrecioUnitario = 32_000m, Cantidad = 2 });

        var carrito = svc.ObtenerCarrito(sesion);
        // 28.000 + (32.000 × 2) = 92.000
        Assert.Equal(92_000m, carrito.Subtotal);
    }

    // ── Criterio: eliminar ítem ─────────────────────────────────────────

    [Fact]
    public void EliminarItem_LoQuitaDelCarrito()
    {
        var svc    = CrearServicio();
        var sesion = CrearSesion();
        var item   = ItemEjemplo();
        svc.AgregarItem(sesion, item);

        var eliminado = svc.EliminarItem(sesion, item.Id);

        Assert.True(eliminado);
        Assert.True(svc.ObtenerCarrito(sesion).EstaVacio);
    }

    [Fact]
    public void EliminarItemInexistente_DevuelveFalse()
    {
        var svc    = CrearServicio();
        var sesion = CrearSesion();

        var resultado = svc.EliminarItem(sesion, Guid.NewGuid());

        Assert.False(resultado);
    }

    // ── Criterio: cambiar cantidad ──────────────────────────────────────

    [Fact]
    public void CambiarCantidad_ActualizaElItem()
    {
        var svc    = CrearServicio();
        var sesion = CrearSesion();
        var item   = ItemEjemplo();
        svc.AgregarItem(sesion, item);

        svc.CambiarCantidad(sesion, item.Id, 3);

        var carrito = svc.ObtenerCarrito(sesion);
        Assert.Equal(3, carrito.Items.First().Cantidad);
        Assert.Equal(28_000m * 3, carrito.Items.First().Subtotal);
    }

    [Fact]
    public void CambiarCantidadACero_EliminaElItem()
    {
        var svc    = CrearServicio();
        var sesion = CrearSesion();
        var item   = ItemEjemplo();
        svc.AgregarItem(sesion, item);

        svc.CambiarCantidad(sesion, item.Id, 0);

        Assert.True(svc.ObtenerCarrito(sesion).EstaVacio);
    }

    // ── Criterio: vaciar carrito tras confirmación ──────────────────────

    [Fact]
    public void VaciarCarrito_DejaCarritoVacio()
    {
        var svc    = CrearServicio();
        var sesion = CrearSesion();
        svc.AgregarItem(sesion, ItemEjemplo());
        svc.AgregarItem(sesion, ItemEjemplo("Mexicana"));

        svc.VaciarCarrito(sesion);

        Assert.True(svc.ObtenerCarrito(sesion).EstaVacio);
    }

    // ── Criterio: número de pedido generado ────────────────────────────

    [Fact]
    public void GenerarNumeroPedido_TieneFormatoEsperado()
    {
        var svc = CrearServicio();
        var numero = svc.GenerarNumeroPedido();

        Assert.StartsWith("PZ-", numero);
        Assert.Matches(@"^PZ-\d{8}-[A-Z0-9]{4}$", numero);
    }

    [Fact]
    public void GenerarNumeroPedido_EsUnico()
    {
        var svc = CrearServicio();
        var n1 = svc.GenerarNumeroPedido();
        var n2 = svc.GenerarNumeroPedido();
        Assert.NotEqual(n1, n2);
    }

    // ── Criterio: no confirmar si el carrito está vacío ─────────────────

    [Fact]
    public void CarritoVacio_EstaVacioEsTrue()
    {
        var carrito = new Carrito();
        Assert.True(carrito.EstaVacio);
    }

    [Fact]
    public void CarritoConItems_EstaVacioEsFalse()
    {
        var carrito = new Carrito();
        carrito.Items.Add(ItemEjemplo());
        Assert.False(carrito.EstaVacio);
    }

    // ── Criterio: validación de campos obligatorios (ConfirmarPedidoViewModel) ──

    [Fact]
    public void ConfirmarPedidoSinNombre_FallaValidacion()
    {
        var vm = new ConfirmarPedidoViewModel
        {
            Nombre   = "",
            Telefono = "3001234567",
            Carrito  = new Carrito { Items = new List<ItemCarrito> { ItemEjemplo() } },
        };

        var errores = ValidarViewModel(vm);
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(ConfirmarPedidoViewModel.Nombre)));
    }

    [Fact]
    public void ConfirmarPedidoSinTelefono_FallaValidacion()
    {
        var vm = new ConfirmarPedidoViewModel
        {
            Nombre   = "Ana García",
            Telefono = "",
            Carrito  = new Carrito { Items = new List<ItemCarrito> { ItemEjemplo() } },
        };

        var errores = ValidarViewModel(vm);
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(ConfirmarPedidoViewModel.Telefono)));
    }

    [Fact]
    public void ConfirmarDomicilioSinDireccion_FallaValidacion()
    {
        var vm = new ConfirmarPedidoViewModel
        {
            Nombre      = "Ana García",
            Telefono    = "3001234567",
            EsDomicilio = true,
            Direccion   = "",
            Carrito     = new Carrito { Items = new List<ItemCarrito> { ItemEjemplo() } },
        };

        var errores = ValidarViewModel(vm);
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(ConfirmarPedidoViewModel.Direccion)));
    }

    [Fact]
    public void ConfirmarTiendaSinDireccion_EsValido()
    {
        var vm = new ConfirmarPedidoViewModel
        {
            Nombre      = "Ana García",
            Telefono    = "3001234567",
            EsDomicilio = false,
            Direccion   = null,
            Carrito     = new Carrito { Items = new List<ItemCarrito> { ItemEjemplo() } },
        };

        var errores = ValidarViewModel(vm);
        Assert.DoesNotContain(errores, e => e.MemberNames.Contains(nameof(ConfirmarPedidoViewModel.Direccion)));
    }

    // ── Helpers privados ────────────────────────────────────────────────

    private static List<System.ComponentModel.DataAnnotations.ValidationResult> ValidarViewModel(object vm)
    {
        var ctx      = new System.ComponentModel.DataAnnotations.ValidationContext(vm);
        var errores  = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(vm, ctx, errores, validateAllProperties: true);
        return errores;
    }
}

// ────────────────────────────────────────────────────────────────────────────
// Implementación mínima de ISession para pruebas unitarias
// ────────────────────────────────────────────────────────────────────────────
internal class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id => Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}
