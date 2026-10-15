using Dapper;
using Npgsql;

namespace PizzaYa.Repositories;

/// <summary>Configuración en Supabase (tabla de database/07_hu7_admin_menu.sql).</summary>
public class SupabaseConfiguracionRepository : IConfiguracionRepository
{
    private readonly NpgsqlDataSource _db;

    public SupabaseConfiguracionRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    public async Task<string?> ObtenerAsync(string clave)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<string?>(
            "select valor from configuracion where clave = @clave", new { clave });
    }

    public async Task GuardarAsync(string clave, string valor)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await conn.ExecuteAsync(
            """
            insert into configuracion (clave, valor) values (@clave, @valor)
            on conflict (clave) do update set valor = excluded.valor, actualizado_en = now()
            """, new { clave, valor });
    }
}
