using System.Globalization;

namespace PizzaYa;

public static class Formato
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    /// <summary>Ej: 28000 -> "$ 28.000"</summary>
    public static string Pesos(decimal valor) => valor.ToString("C0", Colombia);
}
