// Data/ClienteRepository.cs
using System.Data;
using Microsoft.Data.SqlClient;
using CafApi.Models;

namespace CafApi.Data;

public class ClientsRepository
{
    private readonly Db _db;
    public ClientsRepository(Db db) => _db = db;

    public async Task<IEnumerable<Client>> GetAllAsync(string? username = null)
    {
        var list = new List<Client>();
        var sql = @"SELECT ClienteID, Nombre, Contacto, Estado
                    FROM dbo.Cliente";
        if (!string.IsNullOrWhiteSpace(username))
            sql += " WHERE Nombre LIKE @n";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);

        if (!string.IsNullOrWhiteSpace(username))
            cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 24) { Value = $"%{username}%" });

        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new Client
            {
                ClientID = rd.GetInt32(rd.GetOrdinal("ClienteID")),
                Username = rd.GetString(rd.GetOrdinal("Nombre")),
                Contact = rd.IsDBNull(rd.GetOrdinal("Contacto"))
                                ? null
                                : rd.GetString(rd.GetOrdinal("Contacto")),
                State = rd.GetBoolean(rd.GetOrdinal("Estado"))
            });
        }
        return list;
    }

    public async Task<Client?> GetByIdAsync(int id)
    {
        const string sql = @"SELECT ClienteID, Nombre, Contacto, Estado
                             FROM dbo.Cliente WHERE ClienteID=@id";
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new Client
        {
            ClientID = rd.GetInt32(rd.GetOrdinal("ClienteID")),
            Username = rd.GetString(rd.GetOrdinal("Nombre")),
            Contact = rd.IsDBNull(rd.GetOrdinal("Contacto"))
                            ? null
                            : rd.GetString(rd.GetOrdinal("Contacto")),
            State = rd.GetBoolean(rd.GetOrdinal("Estado"))
        };
    }

    public async Task<int> CreateAsync(ClientCreateDto c)
    {
        const string sql = @"
INSERT INTO dbo.Cliente (Nombre, Contacto, Estado)
OUTPUT INSERTED.ClienteID
VALUES (@Nombre, @Contacto, @Estado);";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 24) { Value = c.Username });
        cmd.Parameters.Add(new SqlParameter("@Contacto", SqlDbType.NVarChar, -1) { Value = (object?)c.Contact ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Estado", SqlDbType.Bit) { Value = c.State });
        await con.OpenAsync();

        var obj = await cmd.ExecuteScalarAsync();
        if (obj is int newId) return newId;

        // fallback defensivo (por si el provider devuelve long)
        if (obj != null && int.TryParse(obj.ToString(), out var parsed)) return parsed;

        throw new InvalidOperationException("El INSERT no devolvió ClienteID.");
    }

    public async Task<bool> UpdateAsync(Client c)
    {
        const string sql = @"
UPDATE dbo.Cliente
   SET Nombre=@Nombre, Contacto=@Contacto, Estado=@Estado
 WHERE ClienteID=@ClienteID";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = c.ClientID });
        cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 24) { Value = c.Username});
        cmd.Parameters.Add(new SqlParameter("@Contacto", SqlDbType.NVarChar, -1) { Value = (object?)c.Contact ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Estado", SqlDbType.Bit) { Value = c.State });
        await con.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Validar que no tenga prendas o muestras asociadas
        const string checkSql = @"
        SELECT 
            CASE 
                WHEN EXISTS (SELECT 1 FROM dbo.Prenda WHERE ClienteID = @id) THEN 1
                WHEN EXISTS (SELECT 1 FROM dbo.Muestra WHERE ClienteID = @id) THEN 1
                ELSE 0 
            END;";

        const string deleteSql = @"DELETE FROM dbo.Cliente WHERE ClienteID = @id;";

        using var con = _db.GetConnection();
        await con.OpenAsync();

        using (var checkCmd = new SqlCommand(checkSql, con))
        {
            checkCmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
            var hasRelations = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 1;

            if (hasRelations)
                throw new InvalidOperationException("No se puede eliminar un cliente que tiene prendas o muestras asociadas.");
        }

        using (var deleteCmd = new SqlCommand(deleteSql, con))
        {
            deleteCmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
            return await deleteCmd.ExecuteNonQueryAsync() > 0;
        }
    }

    public async Task<bool> ExistsByNombreAsync(string username, int? excludeId = null)
    {
        var sql = "SELECT 1 FROM dbo.Cliente WHERE Nombre=@n";
        if (excludeId.HasValue) sql += " AND ClienteID<>@id";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 24) { Value = username });
        if (excludeId.HasValue)
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = excludeId.Value });

        await con.OpenAsync();
        var obj = await cmd.ExecuteScalarAsync();
        return obj != null;
    }
}
