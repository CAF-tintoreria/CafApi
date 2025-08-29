// Data/ClienteRepository.cs
using System.Data;
using Microsoft.Data.SqlClient;
using CafApi.Models;

namespace CafApi.Data;

public class ClienteRepository
{
    private readonly Db _db;
    public ClienteRepository(Db db) => _db = db;

    public async Task<IEnumerable<Cliente>> GetAllAsync(string? nombre = null)
    {
        var list = new List<Cliente>();
        var sql = @"SELECT ClienteID, Nombre, Contacto, Estado
                    FROM dbo.Cliente";
        if (!string.IsNullOrWhiteSpace(nombre))
            sql += " WHERE Nombre LIKE @n";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);

        if (!string.IsNullOrWhiteSpace(nombre))
            cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 24) { Value = $"%{nombre}%" });

        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new Cliente
            {
                ClienteID = rd.GetInt32(rd.GetOrdinal("ClienteID")),
                Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
                Contacto = rd.IsDBNull(rd.GetOrdinal("Contacto"))
                                ? null
                                : rd.GetString(rd.GetOrdinal("Contacto")),
                Estado = rd.GetBoolean(rd.GetOrdinal("Estado"))
            });
        }
        return list;
    }

    public async Task<Cliente?> GetByIdAsync(int id)
    {
        const string sql = @"SELECT ClienteID, Nombre, Contacto, Estado
                             FROM dbo.Cliente WHERE ClienteID=@id";
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new Cliente
        {
            ClienteID = rd.GetInt32(rd.GetOrdinal("ClienteID")),
            Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
            Contacto = rd.IsDBNull(rd.GetOrdinal("Contacto"))
                            ? null
                            : rd.GetString(rd.GetOrdinal("Contacto")),
            Estado = rd.GetBoolean(rd.GetOrdinal("Estado"))
        };
    }

    public async Task<int> CreateAsync(ClienteCreateDto c)
    {
        const string sql = @"
INSERT INTO dbo.Cliente (Nombre, Contacto, Estado)
OUTPUT INSERTED.ClienteID
VALUES (@Nombre, @Contacto, @Estado);";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 24) { Value = c.Nombre });
        cmd.Parameters.Add(new SqlParameter("@Contacto", SqlDbType.NVarChar, -1) { Value = (object?)c.Contacto ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Estado", SqlDbType.Bit) { Value = c.Estado });
        await con.OpenAsync();

        var obj = await cmd.ExecuteScalarAsync();
        if (obj is int newId) return newId;

        // fallback defensivo (por si el provider devuelve long)
        if (obj != null && int.TryParse(obj.ToString(), out var parsed)) return parsed;

        throw new InvalidOperationException("El INSERT no devolvió ClienteID.");
    }

    public async Task<bool> UpdateAsync(Cliente c)
    {
        const string sql = @"
UPDATE dbo.Cliente
   SET Nombre=@Nombre, Contacto=@Contacto, Estado=@Estado
 WHERE ClienteID=@ClienteID";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = c.ClienteID });
        cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 24) { Value = c.Nombre });
        cmd.Parameters.Add(new SqlParameter("@Contacto", SqlDbType.NVarChar, -1) { Value = (object?)c.Contacto ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Estado", SqlDbType.Bit) { Value = c.Estado });
        await con.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = @"DELETE FROM dbo.Cliente WHERE ClienteID=@id";
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        await con.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExistsByNombreAsync(string nombre, int? excludeId = null)
    {
        var sql = "SELECT 1 FROM dbo.Cliente WHERE Nombre=@n";
        if (excludeId.HasValue) sql += " AND ClienteID<>@id";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 24) { Value = nombre });
        if (excludeId.HasValue)
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = excludeId.Value });

        await con.OpenAsync();
        var obj = await cmd.ExecuteScalarAsync();
        return obj != null;
    }
}
