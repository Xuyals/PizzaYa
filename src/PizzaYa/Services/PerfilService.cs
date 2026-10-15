using Microsoft.AspNetCore.Http;
using PizzaYa.Models;
using PizzaYa.Repositories;

namespace PizzaYa.Services;

/// <summary>HU6 – Perfil del cliente registrado: favoritas, último pedido y cupones.</summary>
public class PerfilService
{
    private readonly IFavoritaRepository _favoritas;
    private readonly IPedidoRepository _pedidos;
    private readonly IMenuRepository _menu;
    private readonly ArmadoService _armado;
    private readonly CarritoService _carrito;
    private readonly CuponService _cupones;

    public PerfilService(
        IFavoritaRepository favoritas, IPedidoRepository pedidos, IMenuRepository menu,
        ArmadoService armado, CarritoService carrito, CuponService cupones)
    {
        _favoritas = favoritas;
        _pedidos = pedidos;
        _menu = menu;
        _armado = armado;
        _carrito = carrito;
        _cupones = cupones;
    }

    /// <summary>Arma todo lo que muestra el perfil. Con listas vacías el perfil se muestra "sin datos".</summary>
    public async Task<PerfilViewModel> ObtenerPerfilAsync(int usuarioId, string nombre, DateTime? ahora = null)
    {
        var perfil = new PerfilViewModel
        {
            Nombre = nombre,
            UltimoPedido = await _pedidos.ObtenerUltimoPorUsuarioAsync(usuarioId),
            Cupones = await _cupones.ObtenerVigentesAsync(usuarioId, ahora),
        };

        var ingredientes = (await _menu.ObtenerIngredientesAsync()).ToDictionary(i => i.Id);
        foreach (var favorita in await _favoritas.ObtenerPorUsuarioAsync(usuarioId))
        {
            var item = await _armado.ConstruirItemAsync(favorita.ComoSeleccion());
            if (item is null) continue; // la pizza ya no está en el menú

            perfil.Favoritas.Add(new FavoritaVista
            {
                Favorita = favorita,
                NombrePizza = item.NombrePizza,
                NombreTamano = item.NombreTamano,
                NombreBorde = item.NombreBorde,
                Extras = item.NombresExtras,
                Quitados = favorita.QuitadosIds.Where(ingredientes.ContainsKey).Select(id => ingredientes[id].Nombre).ToList(),
                Precio = item.PrecioUnitario,
            });
        }

        return perfil;
    }

    /// <summary>Guarda la pizza configurada como favorita. Devuelve null si la configuración no es válida.</summary>
    public async Task<Favorita?> MarcarFavoritaAsync(int usuarioId, PizzaPersonalizada seleccion)
    {
        if (seleccion.TamanoId is null || seleccion.BordeId is null) return null;
        if (await _armado.ConstruirItemAsync(seleccion) is null) return null;

        return await _favoritas.AgregarAsync(new Favorita
        {
            UsuarioId = usuarioId,
            PizzaId = seleccion.PizzaId,
            TamanoId = seleccion.TamanoId.Value,
            BordeId = seleccion.BordeId.Value,
            ExtrasIds = seleccion.ExtrasIds.Distinct().ToArray(),
            QuitadosIds = seleccion.QuitadosIds.Distinct().ToArray(),
        });
    }

    /// <summary>"Pedir de nuevo" en un clic: pone la favorita en el carrito. Devuelve false si ya no se puede.</summary>
    public async Task<bool> PedirFavoritaAsync(int usuarioId, int favoritaId, ISession sesion)
    {
        var favorita = await _favoritas.ObtenerAsync(favoritaId, usuarioId);
        if (favorita is null) return false;

        var item = await _armado.ConstruirItemAsync(favorita.ComoSeleccion());
        if (item is null) return false;

        _carrito.AgregarItem(sesion, item);
        return true;
    }

    /// <summary>
    /// Repite el último pedido: pone sus pizzas en el carrito con el precio de hoy.
    /// Devuelve cuántas pizzas agregó y cuántas ya no existen en el menú.
    /// </summary>
    public async Task<(int Agregadas, int Omitidas)> RepetirUltimoPedidoAsync(int usuarioId, ISession sesion)
    {
        var pedido = await _pedidos.ObtenerUltimoPorUsuarioAsync(usuarioId);
        if (pedido is null) return (0, 0);

        int agregadas = 0, omitidas = 0;
        foreach (var linea in pedido.Items)
        {
            var seleccion = new PizzaPersonalizada
            {
                PizzaId = linea.PizzaId,
                TamanoId = linea.TamanoId,
                BordeId = linea.BordeId,
                ExtrasIds = linea.ExtrasIds.ToList(),
                QuitadosIds = linea.QuitadosIds.ToList(),
            };
            var item = await _armado.ConstruirItemAsync(seleccion, linea.Cantidad);
            if (item is null) { omitidas++; continue; }
            _carrito.AgregarItem(sesion, item);
            agregadas++;
        }
        return (agregadas, omitidas);
    }
}
