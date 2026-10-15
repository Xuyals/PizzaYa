namespace PizzaYa.Models;

/// <summary>
/// Resultado de la HU1: la pizza tal como la armó el cliente.
/// Es el objeto que reciben el Equipo 2 (precio y carrito) y el Equipo 3 (pedidos).
/// Se guarda en la sesión con la clave <see cref="ClaveSesion"/>.
/// </summary>
public class PizzaPersonalizada
{
    public const string ClaveSesion = "PizzaPersonalizada";
    public const int MaximoExtras = 6;

    public int PizzaId { get; set; }
    public int? TamanoId { get; set; }
    public int? BordeId { get; set; }
    public List<int> ExtrasIds { get; set; } = new();
    public List<int> QuitadosIds { get; set; } = new();
}
