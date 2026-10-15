// Data/PrecioClienteRepository.cs
using System.Data;
using Microsoft.Data.SqlClient;
using CafApi.Models;

namespace CafApi.Data;

public class PrecioClienteRepository
{
    private readonly Db _db;
    public PrecioClienteRepository(Db db) => _db = db;

    // ---------- Listado de precios de un cliente ----------
    public async Task<IEnumerable<PrecioCliente>> GetByClienteAsync(int clienteId)
    {
        const string sql = @"
SELECT p.PrecioClienteID, p.ClienteID, p.TipoPrenda, p.UnidadCobro, p.Precio,
       p.Notas, p.FechaActualizacion,
       pr.ProcesoID, pr.TipoProceso
FROM dbo.PrecioCliente p
LEFT JOIN dbo.PrecioClienteProceso pcp ON pcp.PrecioClienteID = p.PrecioClienteID
LEFT JOIN dbo.Proceso pr ON pr.ProcesoID = pcp.ProcesoID
WHERE p.ClienteID = @ClienteID
ORDER BY p.TipoPrenda, p.PrecioClienteID, pcp.PrecioClienteProcesoID;";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = clienteId });
        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        return await ReadPreciosAsync(rd);
    }

    // ---------- Un precio por Id ----------
    public async Task<PrecioCliente?> GetByIdAsync(int precioClienteId)
    {
        const string sql = @"
SELECT p.PrecioClienteID, p.ClienteID, p.TipoPrenda, p.UnidadCobro, p.Precio,
       p.Notas, p.FechaActualizacion,
       pr.ProcesoID, pr.TipoProceso
FROM dbo.PrecioCliente p
LEFT JOIN dbo.PrecioClienteProceso pcp ON pcp.PrecioClienteID = p.PrecioClienteID
LEFT JOIN dbo.Proceso pr ON pr.ProcesoID = pcp.ProcesoID
WHERE p.PrecioClienteID = @Id
ORDER BY pcp.PrecioClienteProcesoID;";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = precioClienteId });
        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        return (await ReadPreciosAsync(rd)).FirstOrDefault();
    }

    // ---------- Crear (precio + sus procesos, en una transaccion) ----------
    public async Task<int> CreateAsync(int clienteId, PrecioClienteCreateDto dto)
    {
        const string insertPrecio = @"
INSERT INTO dbo.PrecioCliente (ClienteID, TipoPrenda, UnidadCobro, Precio, Notas)
OUTPUT INSERTED.PrecioClienteID
VALUES (@ClienteID, @TipoPrenda, @UnidadCobro, @Precio, @Notas);";

        using var con = _db.GetConnection();
        await con.OpenAsync();
        using var tx = con.BeginTransaction();

        try
        {
            int newId;
            using (var cmd = new SqlCommand(insertPrecio, con, tx))
            {
                cmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = clienteId });
                cmd.Parameters.Add(new SqlParameter("@TipoPrenda", SqlDbType.NVarChar, 50) { Value = dto.TipoPrenda });
                cmd.Parameters.Add(new SqlParameter("@UnidadCobro", SqlDbType.VarChar, 10) { Value = dto.UnidadCobro });
                cmd.Parameters.Add(new SqlParameter("@Precio", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = dto.Precio });
                cmd.Parameters.Add(new SqlParameter("@Notas", SqlDbType.NVarChar, 250) { Value = (object?)dto.Notas ?? DBNull.Value });
                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            await InsertProcesosAsync(con, tx, newId, dto.ProcesoIds);

            await tx.CommitAsync();
            return newId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ---------- Editar (reemplaza los procesos por los nuevos) ----------
    public async Task<bool> UpdateAsync(int precioClienteId, PrecioClienteUpdateDto dto)
    {
        const string updatePrecio = @"
UPDATE dbo.PrecioCliente
   SET TipoPrenda = @TipoPrenda,
       UnidadCobro = @UnidadCobro,
       Precio = @Precio,
       Notas = @Notas,
       FechaActualizacion = GETDATE()
 WHERE PrecioClienteID = @Id;";

        const string deleteProcesos = @"
DELETE FROM dbo.PrecioClienteProceso WHERE PrecioClienteID = @Id;";

        using var con = _db.GetConnection();
        await con.OpenAsync();
        using var tx = con.BeginTransaction();

        try
        {
            int rows;
            using (var cmd = new SqlCommand(updatePrecio, con, tx))
            {
                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = precioClienteId });
                cmd.Parameters.Add(new SqlParameter("@TipoPrenda", SqlDbType.NVarChar, 50) { Value = dto.TipoPrenda });
                cmd.Parameters.Add(new SqlParameter("@UnidadCobro", SqlDbType.VarChar, 10) { Value = dto.UnidadCobro });
                cmd.Parameters.Add(new SqlParameter("@Precio", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = dto.Precio });
                cmd.Parameters.Add(new SqlParameter("@Notas", SqlDbType.NVarChar, 250) { Value = (object?)dto.Notas ?? DBNull.Value });
                rows = await cmd.ExecuteNonQueryAsync();
            }

            if (rows == 0)
            {
                await tx.RollbackAsync();
                return false;
            }

            using (var del = new SqlCommand(deleteProcesos, con, tx))
            {
                del.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = precioClienteId });
                await del.ExecuteNonQueryAsync();
            }

            await InsertProcesosAsync(con, tx, precioClienteId, dto.ProcesoIds);

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ---------- Eliminar (los procesos se borran solos por ON DELETE CASCADE) ----------
    public async Task<bool> DeleteAsync(int precioClienteId)
    {
        const string sql = "DELETE FROM dbo.PrecioCliente WHERE PrecioClienteID = @Id;";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = precioClienteId });
        await con.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // ---------- Cantidad de precios por cliente (columna "Precios" de la lista) ----------
    public async Task<Dictionary<int, int>> CountByClienteAsync()
    {
        const string sql = @"
SELECT ClienteID, COUNT(*) AS Cantidad
FROM dbo.PrecioCliente
GROUP BY ClienteID;";

        var result = new Dictionary<int, int>();
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            result[rd.GetInt32(0)] = rd.GetInt32(1);
        return result;
    }

    // ---------- Sugerencias al escribir el tipo de prenda ----------
    public async Task<IEnumerable<string>> GetTiposPrendaAsync()
    {
        const string sql = @"
SELECT DISTINCT TipoPrenda
FROM dbo.PrecioCliente
ORDER BY TipoPrenda;";

        var list = new List<string>();
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            list.Add(rd.GetString(0));
        return list;
    }

    // ---------- Helpers ----------
    private static async Task InsertProcesosAsync(SqlConnection con, SqlTransaction tx, int precioClienteId, IEnumerable<int> procesoIds)
    {
        const string sql = @"
INSERT INTO dbo.PrecioClienteProceso (PrecioClienteID, ProcesoID)
VALUES (@PrecioClienteID, @ProcesoID);";

        foreach (var procesoId in procesoIds.Distinct())
        {
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.Add(new SqlParameter("@PrecioClienteID", SqlDbType.Int) { Value = precioClienteId });
            cmd.Parameters.Add(new SqlParameter("@ProcesoID", SqlDbType.Int) { Value = procesoId });
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // Lee filas "precio + proceso" y las agrupa: un precio con su lista de procesos
    private static async Task<List<PrecioCliente>> ReadPreciosAsync(SqlDataReader rd)
    {
        var byId = new Dictionary<int, PrecioCliente>();
        var ordered = new List<PrecioCliente>();

        int oId = rd.GetOrdinal("PrecioClienteID");
        int oCliente = rd.GetOrdinal("ClienteID");
        int oTipo = rd.GetOrdinal("TipoPrenda");
        int oUnidad = rd.GetOrdinal("UnidadCobro");
        int oPrecio = rd.GetOrdinal("Precio");
        int oNotas = rd.GetOrdinal("Notas");
        int oFecha = rd.GetOrdinal("FechaActualizacion");
        int oProcesoId = rd.GetOrdinal("ProcesoID");
        int oProceso = rd.GetOrdinal("TipoProceso");

        while (await rd.ReadAsync())
        {
            var id = rd.GetInt32(oId);

            if (!byId.TryGetValue(id, out var item))
            {
                item = new PrecioCliente
                {
                    PrecioClienteID = id,
                    ClienteID = rd.GetInt32(oCliente),
                    TipoPrenda = rd.GetString(oTipo),
                    UnidadCobro = rd.GetString(oUnidad),
                    Precio = rd.GetDecimal(oPrecio),
                    Notas = rd.IsDBNull(oNotas) ? null : rd.GetString(oNotas),
                    FechaActualizacion = rd.GetDateTime(oFecha),
                    Procesos = new List<PrecioProcesoItem>()
                };
                byId.Add(id, item);
                ordered.Add(item);
            }

            if (!rd.IsDBNull(oProcesoId))
            {
                item.Procesos.Add(new PrecioProcesoItem
                {
                    ProcesoID = rd.GetInt32(oProcesoId),
                    TipoProceso = rd.IsDBNull(oProceso) ? "" : rd.GetString(oProceso)
                });
            }
        }

        return ordered;
    }
}
