using PizzaYa.Models;
using PizzaYa.Repositories;

namespace PizzaYa.Services;

/// <summary>
/// HU7 – Reglas para registrar y editar pizzas, bordes, extras y tamaños. Cada operación valida primero
/// y devuelve los campos con error (para resaltarlos); si hay errores no se guarda nada.
/// </summary>
public class MenuAdminService
{
    public const int MaximoNombre = 80;
    public const int MaximoDescripcion = 300;
    public const decimal MaximoPrecio = 1_000_000m;
    public const decimal MaximoFactor = 10m;

    private readonly IMenuAdminRepository _admin;
    private readonly IMenuRepository _menu;

    public MenuAdminService(IMenuAdminRepository admin, IMenuRepository menu)
    {
        _admin = admin;
        _menu = menu;
    }

    public record ErrorCampo(string Campo, string Mensaje);

    /// <summary>Resultado de guardar: sin errores = se guardó.</summary>
    public record Resultado(List<ErrorCampo> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
        public static Resultado Ok() => new(new List<ErrorCampo>());
        public static Resultado Falla(string campo, string mensaje) => new(new List<ErrorCampo> { new(campo, mensaje) });
    }

    // ── Pizzas ──────────────────────────────────────────────────────────

    /// <summary>Registra (id null) o edita una pizza. Hace falta nombre, precio y al menos un ingrediente base.</summary>
    public async Task<Resultado> GuardarPizzaAsync(
        int? id, string? nombre, string? descripcion, decimal? precioBase, IEnumerable<int>? ingredientesBaseIds)
    {
        var errores = new List<ErrorCampo>();
        ValidarNombre(nombre, errores, "El nombre de la pizza es obligatorio.");
        ValidarPrecio(precioBase, errores, nameof(Pizza.PrecioBase), "El precio de la pizza es obligatorio.", positivo: true);

        if (!string.IsNullOrEmpty(descripcion) && descripcion.Length > MaximoDescripcion)
            errores.Add(new(nameof(Pizza.Descripcion), $"La descripción no puede pasar de {MaximoDescripcion} caracteres."));

        var ids = (ingredientesBaseIds ?? Array.Empty<int>()).Distinct().ToList();
        if (ids.Count == 0)
            errores.Add(new("IngredientesBaseIds", "Escoge al menos un ingrediente base."));
        else
        {
            var existentes = (await _menu.ObtenerIngredientesAsync()).Select(i => i.Id).ToHashSet();
            if (ids.Any(i => !existentes.Contains(i)))
                errores.Add(new("IngredientesBaseIds", "Uno de los ingredientes escogidos no existe."));
        }

        if (errores.Count > 0) return new Resultado(errores);

        var limpio = nombre!.Trim();
        var detalle = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

        if (id is null)
        {
            var creada = await _admin.CrearPizzaAsync(
                new Pizza { Nombre = limpio, Descripcion = detalle, PrecioBase = precioBase!.Value, Disponible = true }, ids);
            return creada is null ? Resultado.Falla(nameof(Pizza.Nombre), "Ya existe una pizza con ese nombre.") : Resultado.Ok();
        }

        var editada = await _admin.ActualizarPizzaAsync(id.Value, limpio, detalle, precioBase!.Value, ids);
        return editada ? Resultado.Ok() : Resultado.Falla(nameof(Pizza.Nombre), "No se pudo guardar: la pizza no existe o ese nombre ya está en uso.");
    }

    /// <summary>Oculta la pizza del menú del cliente (false) o vuelve a mostrarla (true).</summary>
    public Task<bool> CambiarDisponibilidadAsync(int id, bool disponible) =>
        _admin.CambiarDisponibilidadPizzaAsync(id, disponible);

    // ── Bordes, extras y tamaños ────────────────────────────────────────

    public Task<Resultado> GuardarBordeAsync(int? id, string? nombre, decimal? precio) =>
        GuardarSimpleAsync(nombre, precio, "El precio del borde es obligatorio.", positivo: false, MaximoPrecio,
            crear: (n, p) => _admin.CrearBordeAsync(n, p).ContinueWith(t => t.Result is not null),
            editar: (n, p) => _admin.ActualizarBordeAsync(id!.Value, n, p), id, "borde");

    public Task<Resultado> GuardarIngredienteAsync(int? id, string? nombre, decimal? precioExtra) =>
        GuardarSimpleAsync(nombre, precioExtra, "El precio del extra es obligatorio.", positivo: false, MaximoPrecio,
            crear: (n, p) => _admin.CrearIngredienteAsync(n, p).ContinueWith(t => t.Result is not null),
            editar: (n, p) => _admin.ActualizarIngredienteAsync(id!.Value, n, p), id, "ingrediente");

    public Task<Resultado> GuardarTamanoAsync(int? id, string? nombre, decimal? factorPrecio) =>
        GuardarSimpleAsync(nombre, factorPrecio, "El factor de precio del tamaño es obligatorio.", positivo: true, MaximoFactor,
            crear: (n, p) => _admin.CrearTamanoAsync(n, decimal.Round(p, 2)).ContinueWith(t => t.Result is not null),
            editar: (n, p) => _admin.ActualizarTamanoAsync(id!.Value, n, decimal.Round(p, 2)), id, "tamaño");

    private static async Task<Resultado> GuardarSimpleAsync(
        string? nombre, decimal? valor, string mensajeValor, bool positivo, decimal maximo,
        Func<string, decimal, Task<bool>> crear, Func<string, decimal, Task<bool>> editar, int? id, string que)
    {
        var errores = new List<ErrorCampo>();
        ValidarNombre(nombre, errores, $"El nombre del {que} es obligatorio.");
        ValidarPrecio(valor, errores, "Valor", mensajeValor, positivo, maximo);
        if (errores.Count > 0) return new Resultado(errores);

        var ok = id is null ? await crear(nombre!.Trim(), valor!.Value) : await editar(nombre!.Trim(), valor!.Value);
        return ok ? Resultado.Ok() : Resultado.Falla(nameof(Pizza.Nombre), $"No se pudo guardar: ya existe un {que} con ese nombre o no existe.");
    }

    // ── Validaciones ────────────────────────────────────────────────────

    private static void ValidarNombre(string? nombre, List<ErrorCampo> errores, string mensajeVacio)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            errores.Add(new(nameof(Pizza.Nombre), mensajeVacio));
        else if (nombre.Trim().Length > MaximoNombre)
            errores.Add(new(nameof(Pizza.Nombre), $"El nombre no puede pasar de {MaximoNombre} caracteres."));
    }

    private static void ValidarPrecio(
        decimal? valor, List<ErrorCampo> errores, string campo, string mensajeVacio, bool positivo, decimal maximo = MaximoPrecio)
    {
        if (valor is null)
            errores.Add(new(campo, mensajeVacio));
        else if (positivo ? valor <= 0 : valor < 0)
            errores.Add(new(campo, positivo ? "El valor debe ser mayor a 0." : "El valor no puede ser negativo."));
        else if (valor > maximo)
            errores.Add(new(campo, $"El valor no puede pasar de {maximo:N0}."));
    }
}
