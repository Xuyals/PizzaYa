using Dapper;
using Npgsql;
using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Favoritas en Supabase (tabla de database/06_hu6_perfil.sql).</summary>
public class SupabaseFavoritaRepository : IFavoritaRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public SupabaseFavoritaRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<Favorita>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        return (await conexion.QueryAsync<Favorita>(
            "select * from favoritas where usuario_id = @UsuarioId order by creado_en desc, id desc;",
            new { UsuarioId = usuarioId })).ToList();
    }

    public async Task<Favorita?> ObtenerAsync(int id, int usuarioId)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        return await conexion.QuerySingleOrDefaultAsync<Favorita>(
            "select * from favoritas where id = @Id and usuario_id = @UsuarioId;", new { Id = id, UsuarioId = usuarioId });
    }

    public async Task<Favorita> AgregarAsync(Favorita favorita)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        var extras = favorita.ExtrasIds.OrderBy(x => x).ToArray();
        var quitados = favorita.QuitadosIds.OrderBy(x => x).ToArray();
        var parametros = new
        {
            favorita.UsuarioId, favorita.PizzaId, favorita.TamanoId, favorita.BordeId,
            Extras = extras, Quitados = quitados, favorita.CreadoEn,
        };

        // El índice único evita la duplicada; si ya existía se devuelve esa
        var id = await conexion.ExecuteScalarAsync<int?>(
            @"insert into favoritas (usuario_id, pizza_id, tamano_id, borde_id, extras_ids, quitados_ids, creado_en)
              values (@UsuarioId, @PizzaId, @TamanoId, @BordeId, @Extras, @Quitados, @CreadoEn)
              on conflict do nothing
              returning id;", parametros);

        if (id is not null)
        {
            favorita.Id = id.Value;
            return favorita;
        }

        return await conexion.QuerySingleAsync<Favorita>(
            @"select * from favoritas
              where usuario_id = @UsuarioId and pizza_id = @PizzaId and tamano_id = @TamanoId and borde_id = @BordeId
                and extras_ids = @Extras and quitados_ids = @Quitados;", parametros);
    }

    public async Task<bool> EliminarAsync(int id, int usuarioId)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        return await conexion.ExecuteAsync(
            "delete from favoritas where id = @Id and usuario_id = @UsuarioId;", new { Id = id, UsuarioId = usuarioId }) > 0;
    }
}
