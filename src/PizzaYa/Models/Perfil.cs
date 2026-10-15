namespace PizzaYa.Models;

/// <summary>Pizza que un cliente guardó como favorita (HU6): la misma configuración de la HU1.</summary>
public class Favorita
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int PizzaId { get; set; }
    public int TamanoId { get; set; }
    public int BordeId { get; set; }
    public int[] ExtrasIds { get; set; } = Array.Empty<int>();
    public int[] QuitadosIds { get; set; } = Array.Empty<int>();
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public PizzaPersonalizada ComoSeleccion() => new()
    {
        PizzaId = PizzaId,
        TamanoId = TamanoId,
        BordeId = BordeId,
        ExtrasIds = ExtrasIds.ToList(),
        QuitadosIds = QuitadosIds.ToList(),
    };
}

/// <summary>Una favorita lista para mostrar: con los nombres y el precio de hoy.</summary>
public class FavoritaVista
{
    public Favorita Favorita { get; set; } = new();
    public string NombrePizza { get; set; } = "";
    public string NombreTamano { get; set; } = "";
    public string NombreBorde { get; set; } = "";
    public List<string> Extras { get; set; } = new();
    public List<string> Quitados { get; set; } = new();
    public decimal Precio { get; set; }
}

/// <summary>Un cupón del cliente, vigente y listo para aplicar.</summary>
public class CuponVista
{
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime? VenceEn { get; set; }
}

public class PerfilViewModel
{
    public string Nombre { get; set; } = "";
    public List<FavoritaVista> Favoritas { get; set; } = new();
    public Pedido? UltimoPedido { get; set; }
    public List<CuponVista> Cupones { get; set; } = new();

    /// <summary>Sin favoritas ni pedidos: se muestra un mensaje en lugar de las secciones vacías.</summary>
    public bool SinDatos => Favoritas.Count == 0 && UltimoPedido is null;
}
