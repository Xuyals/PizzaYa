using System.Globalization;
using PizzaYa.Repositories;

namespace PizzaYa.Services;

/// <summary>HU7 – Valor del domicilio que se suma a los pedidos a domicilio (lo cambia el administrador).</summary>
public class ConfiguracionService
{
    public const string ClaveCostoDomicilio = "costo_domicilio";
    public const decimal MaximoCostoDomicilio = 100_000m;

    private readonly IConfiguracionRepository _configuracion;

    public ConfiguracionService(IConfiguracionRepository configuracion)
    {
        _configuracion = configuracion;
    }

    /// <summary>Valor actual del domicilio; si nunca se ha configurado, el valor por defecto de la HU2.</summary>
    public async Task<decimal> ObtenerCostoDomicilioAsync()
    {
        var texto = await _configuracion.ObtenerAsync(ClaveCostoDomicilio);
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) && valor >= 0
            ? valor
            : PrecioService.CostoDomicilioFijo;
    }

    /// <summary>Error de validación del valor, o null si se puede guardar.</summary>
    public static string? Validar(decimal? valor)
    {
        if (valor is null) return "Escribe el valor del domicilio.";
        if (valor < 0) return "El valor del domicilio no puede ser negativo.";
        if (valor > MaximoCostoDomicilio) return $"El valor del domicilio no puede pasar de {MaximoCostoDomicilio:N0}.";
        return null;
    }

    public async Task GuardarCostoDomicilioAsync(decimal valor) =>
        await _configuracion.GuardarAsync(ClaveCostoDomicilio, valor.ToString(CultureInfo.InvariantCulture));
}
