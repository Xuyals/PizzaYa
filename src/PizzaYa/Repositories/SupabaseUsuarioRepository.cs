using Dapper;
using Npgsql;
using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Usuarios en Supabase (tabla de database/08_hu8_usuarios.sql).</summary>
public class SupabaseUsuarioRepository : IUsuarioRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public SupabaseUsuarioRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        return await conexion.QuerySingleOrDefaultAsync<Usuario>(
            "select * from usuarios where lower(correo) = lower(@Correo);", new { Correo = correo?.Trim() });
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        return await conexion.QuerySingleOrDefaultAsync<Usuario>(
            "select * from usuarios where id = @Id;", new { Id = id });
    }

    public async Task<Usuario?> CrearAsync(Usuario usuario)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        // El índice único sobre lower(correo) hace que solo uno de dos registros simultáneos entre
        var id = await conexion.ExecuteScalarAsync<int?>(
            @"insert into usuarios (nombre, correo, password_hash, rol, creado_en)
              values (@Nombre, @Correo, @PasswordHash, @Rol, @CreadoEn)
              on conflict do nothing
              returning id;", usuario);
        if (id is null) return null;
        usuario.Id = id.Value;
        return usuario;
    }
}
