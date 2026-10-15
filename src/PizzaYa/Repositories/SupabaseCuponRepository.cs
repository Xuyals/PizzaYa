using Dapper;
using Npgsql;
using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Cupones de la tabla <c>cupones</c> (HU2) con vencimiento y dueño opcionales (HU6).</summary>
public class SupabaseCuponRepository : ICuponRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public SupabaseCuponRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    private class Fila
    {
        public string Codigo { get; set; } = "";
        public decimal MontoDescuento { get; set; }
        public string Descripcion { get; set; } = "";
        public DateTime? VenceEn { get; set; }
        public int? UsuarioId { get; set; }
    }

    public async Task<List<CuponDescuento>> ObtenerParaUsuarioAsync(int? usuarioId)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        var filas = await conexion.QueryAsync<Fila>(
            @"select codigo, monto_descuento, descripcion, vence_en, usuario_id
              from cupones
              where activo = true and (usuario_id is null or usuario_id = @UsuarioId)
              order by vence_en nulls last, codigo;", new { UsuarioId = usuarioId });
        return filas
            .Select(f => new CuponDescuento(f.Codigo, f.MontoDescuento, f.Descripcion, f.VenceEn, f.UsuarioId))
            .ToList();
    }
}
