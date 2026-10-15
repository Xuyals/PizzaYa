using System.Globalization;

namespace PizzaYa;

public static class FormatoInventario
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    /// <summary>Ej: (4.5, "kg") -> "4,5 kg"</summary>
    public static string Cantidad(decimal valor, string unidad) =>
        $"{valor.ToString("0.##", Colombia)} {unidad}".Trim();
}
