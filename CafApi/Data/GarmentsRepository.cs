using CafApi.Constants.Enum;
using CafApi.Models;
using CafApi.Models.Sample;
using CafApi.Models.Sample;       // por SampleDetailDTO
using CafApi.Models.SampleState;   // por SampleStatus
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System.Data;
using System.Reflection.PortableExecutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CafApi.Data;

public class PrendaRepository
{
    private readonly Db _db;
    public PrendaRepository(Db db) => _db = db;

    public async Task<IEnumerable<Prenda>> GetAllAsync()
    {
        var list = new List<Prenda>();
        const string sql = @"
        SELECT PrendaID, TipoPrenda, NroCorte, CantidadUnidades,
               PesoKilos, CantidadBolsas, FechaIngreso, Remito,
               EstadoPrendaID, ProcesoID, ClienteID, MuestraID, FechaCambio,
               Observaciones
        FROM dbo.Prenda";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        await con.OpenAsync();

        using var rd = await cmd.ExecuteReaderAsync();

        // ——— resolver ordinals UNA vez ———
        int oPrendaID = rd.GetOrdinal("PrendaID");
        int oTipoPrenda = rd.GetOrdinal("TipoPrenda");
        int oNroCorte = rd.GetOrdinal("NroCorte");
        int oCantUnidades = rd.GetOrdinal("CantidadUnidades");
        int oPesoKilos = rd.GetOrdinal("PesoKilos");
        int oCantBolsas = rd.GetOrdinal("CantidadBolsas");
        int oFechaIngreso = rd.GetOrdinal("FechaIngreso");
        int oRemito = rd.GetOrdinal("Remito");
        int oEstadoPrendaID = rd.GetOrdinal("EstadoPrendaID");
        int oProcesoID = rd.GetOrdinal("ProcesoID");
        int oClienteID = rd.GetOrdinal("ClienteID");
        int oMuestraID = rd.GetOrdinal("MuestraID");
        int oFechaCambio = rd.GetOrdinal("FechaCambio");
        int oObservaciones = rd.GetOrdinal("Observaciones");

        while (await rd.ReadAsync())
        {
            var item = new Prenda
            {
                PrendaID = rd.GetInt32(oPrendaID),
                TipoPrenda = rd.IsDBNull(oTipoPrenda) ? null : rd.GetString(oTipoPrenda),
                NroCorte = rd.IsDBNull(oNroCorte) ? (int?)null : rd.GetInt32(oNroCorte),
                CantidadUnidades = rd.IsDBNull(oCantUnidades) ? (int?)null : rd.GetInt32(oCantUnidades),
                PesoKilos = rd.IsDBNull(oPesoKilos) ? (decimal?)null : rd.GetDecimal(oPesoKilos),
                CantidadBolsas = rd.IsDBNull(oCantBolsas) ? (int?)null : rd.GetInt32(oCantBolsas),

                // estos deberían venir NOT NULL según tu esquema
                FechaIngreso = rd.GetDateTime(oFechaIngreso),
                EstadoPrendaID = rd.GetInt32(oEstadoPrendaID),

                // si por algún motivo viniera NULL en datos antiguos,
                // evitamos romper la serialización devolviendo 0
                ProcesoID = rd.IsDBNull(oProcesoID) ? 0 : rd.GetInt32(oProcesoID),
                ClienteID = rd.IsDBNull(oClienteID) ? 0 : rd.GetInt32(oClienteID),

                // opcionales
                MuestraID = rd.IsDBNull(oMuestraID) ? (int?)null : rd.GetInt32(oMuestraID),
                FechaCambio = rd.IsDBNull(oFechaCambio) ? (DateTime?)null : rd.GetDateTime(oFechaCambio),
                Remito = rd.IsDBNull(oRemito) ? null : rd.GetString(oRemito),
                Observaciones = rd.IsDBNull(oObservaciones) ? null : rd.GetString(oObservaciones)
            };

            list.Add(item);
        }

        return list;
    }

    public async Task<Prenda?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT PrendaID, TipoPrenda, NroCorte, CantidadUnidades,
                   PesoKilos, CantidadBolsas, FechaIngreso, Remito,
                   EstadoPrendaID, ProcesoID, ClienteID, MuestraID, FechaCambio,
                   Observaciones
            FROM dbo.Prenda
            WHERE PrendaID = @id";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new Prenda
        {
            PrendaID = rd.GetInt32("PrendaID"),
            TipoPrenda = rd.IsDBNull("TipoPrenda") ? null : rd.GetString("TipoPrenda"),
            NroCorte = rd.IsDBNull("NroCorte") ? (int?)null : rd.GetInt32("NroCorte"),
            CantidadUnidades = rd.IsDBNull("CantidadUnidades") ? (int?)null : rd.GetInt32("CantidadUnidades"),
            PesoKilos = rd.IsDBNull("PesoKilos") ? (decimal?)null : rd.GetDecimal("PesoKilos"),
            CantidadBolsas = rd.IsDBNull("CantidadBolsas") ? (int?)null : rd.GetInt32("CantidadBolsas"),
            FechaIngreso = rd.GetDateTime("FechaIngreso"),
            Remito = rd.IsDBNull("Remito") ? null : rd.GetString("Remito"),
            EstadoPrendaID = rd.GetInt32("EstadoPrendaID"),
            ProcesoID = rd.GetInt32("ProcesoID"),
            ClienteID = rd.GetInt32("ClienteID"),
            MuestraID = rd.IsDBNull("MuestraID") ? (int?)null : rd.GetInt32("MuestraID"),
            FechaCambio = rd.IsDBNull("FechaCambio") ? (DateTime?)null : rd.GetDateTime("FechaCambio"),
            Observaciones = rd.IsDBNull("Observaciones") ? null : rd.GetString("Observaciones")
        };
    }

    public async Task<PrendaEstadoUpdateDto?> GetEstadoByIdAsync(int id)
    {
        const string sql = @"
            SELECT PrendaID, EstadoPrendaID
            FROM dbo.Prenda
            WHERE PrendaID = @id";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new PrendaEstadoUpdateDto
        {
            EstadoPrendaID = rd.GetInt32("EstadoPrendaID")
        };
    }

    public async Task<int> CreateAsync(PrendaCreateDto dto)
    {
        const string insertPrenda = @"
    INSERT INTO dbo.Prenda
      (TipoPrenda, NroCorte, CantidadUnidades, PesoKilos, CantidadBolsas,
       FechaIngreso, Remito, EstadoPrendaID, ProcesoID, ClienteID, MuestraID, Observaciones, FechaCambio)
    OUTPUT INSERTED.PrendaID
    VALUES
      (@TipoPrenda, @NroCorte, @CantidadUnidades, @PesoKilos, @CantidadBolsas,
       @FechaIngreso, @Remito, @EstadoPrendaID, @ProcesoID, @ClienteID, @MuestraID, @Observaciones, NULL);";

        const string insertPP = @"
    INSERT INTO dbo.PrendaProceso (PrendaID, ProcesoID)
    VALUES (@PrendaID, @ProcesoID);";

        using var con = _db.GetConnection();
        await con.OpenAsync();
        using var tx = con.BeginTransaction();

        try
        {
            if (!await IsClientActiveAsync(con, tx, dto.ClienteID))
                throw new InvalidOperationException("El cliente seleccionado está inactivo. No se puede asignar una prenda.");

            int prendaId;
            using (var cmd = new SqlCommand(insertPrenda, con, tx))
            {
                cmd.Parameters.Add(new SqlParameter("@TipoPrenda", SqlDbType.NVarChar, 20) { Value = (object?)dto.TipoPrenda ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@NroCorte", SqlDbType.Int) { Value = (object?)dto.NroCorte ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@CantidadUnidades", SqlDbType.Int) { Value = (object?)dto.CantidadUnidades ?? DBNull.Value });
                var pPesoCreate = new SqlParameter("@PesoKilos", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = (object?)dto.PesoKilos ?? DBNull.Value
                };
                cmd.Parameters.Add(pPesoCreate);
                cmd.Parameters.Add(new SqlParameter("@CantidadBolsas", SqlDbType.Int) { Value = (object?)dto.CantidadBolsas ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@FechaIngreso", SqlDbType.DateTime) { Value = dto.FechaIngreso });
                cmd.Parameters.Add(new SqlParameter("@Remito", SqlDbType.NVarChar, 20) { Value = (object?)dto.Remito ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@EstadoPrendaID", SqlDbType.Int) { Value = dto.EstadoPrendaID });
                cmd.Parameters.Add(new SqlParameter("@ProcesoID", SqlDbType.Int) { Value = dto.ProcesoID });
                cmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = dto.ClienteID });
                cmd.Parameters.Add(new SqlParameter("@MuestraID", SqlDbType.Int) { Value = (object?)dto.MuestraID ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@Observaciones", SqlDbType.NVarChar, 200) { Value = (object?)dto.Observaciones ?? DBNull.Value });

                prendaId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Agregar relación inicial en PrendaProceso
            using (var cmd = new SqlCommand(insertPP, con, tx))
            {
                cmd.Parameters.Add(new SqlParameter("@PrendaID", SqlDbType.Int) { Value = prendaId });
                cmd.Parameters.Add(new SqlParameter("@ProcesoID", SqlDbType.Int) { Value = dto.ProcesoID });
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return prendaId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    public async Task<int> UpdateAsync(int id, PrendaUpdateDto dto)
    {
        const string sql = @"
    UPDATE dbo.Prenda
       SET TipoPrenda = @TipoPrenda,
           NroCorte = @NroCorte,
           CantidadUnidades = @CantidadUnidades,
           PesoKilos = @PesoKilos,
           CantidadBolsas = @CantidadBolsas,
           FechaIngreso = @FechaIngreso,
           Remito = @Remito,
           EstadoPrendaID = @EstadoPrendaID,
           ProcesoID = @ProcesoID,
           ClienteID = @ClienteID,
           MuestraID = @MuestraID,
           Observaciones = @Observaciones,
           FechaCambio = GETDATE()
     WHERE PrendaID = @Id;
        IF NOT EXISTS(
            SELECT 1 FROM dbo.PrendaProceso
            WHERE PrendaID = @Id AND ProcesoID = @ProcesoID
        )
        BEGIN
            INSERT INTO dbo.PrendaProceso(PrendaID, ProcesoID)
            VALUES(@Id, @ProcesoID);
                END
        ";

        using var con = _db.GetConnection();
        await con.OpenAsync();

        if (!await IsClientActiveAsync(con, null, dto.ClienteID))
            throw new InvalidOperationException("El cliente seleccionado está inactivo. No se puede asignar una prenda.");


        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
        cmd.Parameters.Add(new SqlParameter("@TipoPrenda", SqlDbType.NVarChar, 20) { Value = (object?)dto.TipoPrenda ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@NroCorte", SqlDbType.Int) { Value = dto.NroCorte });
        cmd.Parameters.Add(new SqlParameter("@CantidadUnidades", SqlDbType.Int) { Value = dto.CantidadUnidades });
        var pPeso = new SqlParameter("@PesoKilos", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 2,
            Value = dto.PesoKilos
        };
        cmd.Parameters.Add(pPeso);
        cmd.Parameters.Add(new SqlParameter("@CantidadBolsas", SqlDbType.Int) { Value = dto.CantidadBolsas });
        cmd.Parameters.Add(new SqlParameter("@FechaIngreso", SqlDbType.DateTime) { Value = dto.FechaIngreso });
        cmd.Parameters.Add(new SqlParameter("@Remito", SqlDbType.NVarChar, 20) { Value = (object?)dto.Remito ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@EstadoPrendaID", SqlDbType.Int) { Value = dto.EstadoPrendaID });
        cmd.Parameters.Add(new SqlParameter("@ProcesoID", SqlDbType.Int) { Value = dto.ProcesoID });
        cmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = dto.ClienteID });
        cmd.Parameters.Add(new SqlParameter("@MuestraID", SqlDbType.Int) { Value = (object?)dto.MuestraID ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Observaciones", SqlDbType.NVarChar, 200) { Value = (object?)dto.Observaciones ?? DBNull.Value });

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> UpdateEstadoAsync(int prendaId, int nuevoEstadoId)
    {
        const string sql = @"
        -- Actualiza solo si realmente cambia
        UPDATE dbo.Prenda
           SET EstadoPrendaID = @EstadoPrendaID,
               FechaCambio    = GETDATE()
         WHERE PrendaID = @PrendaID
           AND EstadoPrendaID <> @EstadoPrendaID;

        IF @@ROWCOUNT = 0
        BEGIN
            IF EXISTS(SELECT 1 FROM dbo.Prenda WHERE PrendaID = @PrendaID)
                SELECT 0;       -- sin cambio (ya tenía ese estado)
            ELSE
                SELECT -1;      -- no encontrado
        END
        ELSE
            SELECT 1;           -- actualizado
    ";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);

        cmd.Parameters.Add(new SqlParameter("@PrendaID", SqlDbType.Int) { Value = prendaId });
        cmd.Parameters.Add(new SqlParameter("@EstadoPrendaID", SqlDbType.Int) { Value = nuevoEstadoId });

        await con.OpenAsync();
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }


    public async Task<bool> DeleteAsync(int id)
    {
        using var con = _db.GetConnection();
        await con.OpenAsync();
        using var tx = con.BeginTransaction();

        try
        {
            // 1) ¿La prenda existe y su estado es Terminado o Salida?
            const string canDeleteSql = @"
SELECT CASE 
         WHEN EXISTS (
           SELECT 1
           FROM dbo.Prenda p
           JOIN dbo.EstadoPrendas e ON e.EstadoPrendaID = p.EstadoPrendaID
           WHERE p.PrendaID = @id
             AND e.EstadoActual COLLATE Latin1_General_CI_AI IN (N'Terminado', N'Salida')
         ) THEN CAST(1 AS bit)
         ELSE CAST(0 AS bit)
       END;";

            bool canDelete;
            using (var chk = new SqlCommand(canDeleteSql, con, tx))
            {
                chk.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                canDelete = (bool)(await chk.ExecuteScalarAsync() ?? false);
            }

            if (!canDelete)
                throw new InvalidOperationException("Solo se puede eliminar una prenda en estado 'Terminado' o 'Salida'.");

            // 2) Borrar relaciones
            using (var delRel = new SqlCommand("DELETE FROM dbo.PrendaProceso WHERE PrendaID = @id;", con, tx))
            {
                delRel.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                await delRel.ExecuteNonQueryAsync();
            }

            // 3) Borrar la prenda
            int rows;
            using (var delPrenda = new SqlCommand("DELETE FROM dbo.Prenda WHERE PrendaID = @id;", con, tx))
            {
                delPrenda.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                rows = await delPrenda.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return rows > 0;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }



    public async Task<IEnumerable<Prenda>> GetAllWithClientAsync()
    {
        const string sql = @"
SELECT  p.PrendaID, p.TipoPrenda, p.NroCorte, p.CantidadUnidades,
        p.PesoKilos, p.CantidadBolsas, p.FechaIngreso, p.Remito,
        p.EstadoPrendaID, p.ProcesoID, p.MuestraID, p.FechaCambio, p.Observaciones,

        c.ClienteID AS C_ClienteID, c.Nombre AS C_Nombre, c.Contacto AS C_Contacto, c.Estado AS C_Estado,

        m.MuestraID AS M_MuestraID, m.TipoPrenda AS M_TipoPrenda, m.PesoKilos AS M_PesoKilos, m.FechaCambio AS M_FechaCambio,
        em.EstadoMuestraID AS M_EstadoMuestraID, em.Estado AS M_EstadoNombre,

        pp.PrendaProcesoID, pr.ProcesoID AS PR_ProcesoID, pr.TipoProceso AS PR_TipoProceso, pr.Observaciones AS PR_Observaciones
FROM dbo.Prenda p
LEFT JOIN dbo.Cliente       c  ON c.ClienteID       = p.ClienteID
LEFT JOIN dbo.Muestra       m  ON m.MuestraID       = p.MuestraID
LEFT JOIN dbo.EstadoMuestra em ON em.EstadoMuestraID = m.EstadoMuestraID
LEFT JOIN dbo.PrendaProceso pp ON pp.PrendaID       = p.PrendaID
LEFT JOIN dbo.Proceso       pr ON pr.ProcesoID      = pp.ProcesoID
ORDER BY p.PrendaID, pp.PrendaProcesoID;";

        var byId = new Dictionary<int, Prenda>();

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();

        // Ordinals una sola vez
        int oPrendaID = rd.GetOrdinal("PrendaID");
        int oTipoPrenda = rd.GetOrdinal("TipoPrenda");
        int oNroCorte = rd.GetOrdinal("NroCorte");
        int oCantUnidades = rd.GetOrdinal("CantidadUnidades");
        int oPesoKilos = rd.GetOrdinal("PesoKilos");
        int oCantBolsas = rd.GetOrdinal("CantidadBolsas");
        int oFechaIngreso = rd.GetOrdinal("FechaIngreso");
        int oRemito = rd.GetOrdinal("Remito");
        int oEstadoPrendaID = rd.GetOrdinal("EstadoPrendaID");
        int oProcesoID = rd.GetOrdinal("ProcesoID");
        int oMuestraID = rd.GetOrdinal("MuestraID");
        int oFechaCambio = rd.GetOrdinal("FechaCambio");
        int oObservaciones = rd.GetOrdinal("Observaciones");

        int oC_ClienteID = rd.GetOrdinal("C_ClienteID");
        int oC_Nombre = rd.GetOrdinal("C_Nombre");
        int oC_Contacto = rd.GetOrdinal("C_Contacto");
        int oC_Estado = rd.GetOrdinal("C_Estado");

        int oM_MuestraID = rd.GetOrdinal("M_MuestraID");
        int oM_TipoPrenda = rd.GetOrdinal("M_TipoPrenda");
        int oM_PesoKilos = rd.GetOrdinal("M_PesoKilos");
        int oM_FechaCambio = rd.GetOrdinal("M_FechaCambio");
        int oM_EstadoMuestraID = rd.GetOrdinal("M_EstadoMuestraID");
        // int oM_EstadoNombre = rd.GetOrdinal("M_EstadoNombre"); // no lo usamos ahora

        int oPR_ProcesoID = rd.GetOrdinal("PR_ProcesoID");
        int oPR_TipoProceso = rd.GetOrdinal("PR_TipoProceso");
        int oPR_Observaciones = rd.GetOrdinal("PR_Observaciones");

        while (await rd.ReadAsync())
        {
            var id = rd.GetInt32(oPrendaID);

            if (!byId.TryGetValue(id, out var dto))
            {
                dto = new Prenda
                {
                    PrendaID = id,
                    TipoPrenda = rd.IsDBNull(oTipoPrenda) ? null : rd.GetString(oTipoPrenda),
                    NroCorte = rd.IsDBNull(oNroCorte) ? (int?)null : rd.GetInt32(oNroCorte),
                    CantidadUnidades = rd.IsDBNull(oCantUnidades) ? (int?)null : rd.GetInt32(oCantUnidades),
                    PesoKilos = rd.IsDBNull(oPesoKilos) ? (decimal?)null : rd.GetDecimal(oPesoKilos),
                    CantidadBolsas = rd.IsDBNull(oCantBolsas) ? (int?)null : rd.GetInt32(oCantBolsas),
                    FechaIngreso = rd.GetDateTime(oFechaIngreso),
                    Remito = rd.IsDBNull(oRemito) ? null : rd.GetString(oRemito),
                    EstadoPrendaID = rd.GetInt32(oEstadoPrendaID),
                    ProcesoID = rd.GetInt32(oProcesoID),
                    ClienteID = rd.IsDBNull(oC_ClienteID) ? 0 : rd.GetInt32(oC_ClienteID),
                    MuestraID = rd.IsDBNull(oMuestraID) ? (int?)null : rd.GetInt32(oMuestraID),
                    FechaCambio = rd.IsDBNull(oFechaCambio) ? (DateTime?)null : rd.GetDateTime(oFechaCambio),
                    Observaciones = rd.IsDBNull(oObservaciones) ? null : rd.GetString(oObservaciones),
                    Processes = new List<Process>()
                };

                // Cliente (si existe)
                if (!rd.IsDBNull(oC_ClienteID))
                {
                    dto.Cliente = new Client
                    {
                        ClientID = rd.GetInt32(oC_ClienteID),
                        Username = rd.GetString(oC_Nombre),
                        Contact = rd.IsDBNull(oC_Contacto) ? null : rd.GetString(oC_Contacto),
                        State = rd.GetBoolean(oC_Estado)
                    };
                }

                // Muestra (si existe)
                if (!rd.IsDBNull(oM_MuestraID))
                {
                    dto.Muestra = new SampleDetailDTO
                    {
                        MuestraID = rd.GetInt32(oM_MuestraID),
                        ClotheType = rd.IsDBNull(oM_TipoPrenda) ? null : rd.GetString(oM_TipoPrenda),
                        KilogramWeight = rd.IsDBNull(oM_PesoKilos) ? 0 : rd.GetDecimal(oM_PesoKilos),
                        ModifiedAt = rd.IsDBNull(oM_FechaCambio) ? (DateTime?)null : rd.GetDateTime(oM_FechaCambio),
                        Status = new Models.SampleState.SampleStatus
                        {
                            SampleStatusID = rd.IsDBNull(oM_EstadoMuestraID) ? 0 : rd.GetInt32(oM_EstadoMuestraID)
                        }
                    };
                }

                byId.Add(id, dto);
            }

            // Agregar proceso histórico si hay fila en PrendaProceso
            if (!rd.IsDBNull(oPR_ProcesoID))
            {
                dto.Processes!.Add(new Process
                {
                    ProcessID = rd.GetInt32(oPR_ProcesoID),
                    TypeProcess = rd.IsDBNull(oPR_TipoProceso) ? "" : rd.GetString(oPR_TipoProceso),
                    Observations = rd.IsDBNull(oPR_Observaciones) ? null : rd.GetString(oPR_Observaciones)
                });
            }
        }

        return byId.Values;
    }
    public async Task<Prenda?> GetWithClientByIdAsync(int id)
    {
        const string sql = @"
SELECT  p.PrendaID, p.TipoPrenda, p.NroCorte, p.CantidadUnidades,
        p.PesoKilos, p.CantidadBolsas, p.FechaIngreso, p.Remito,
        p.EstadoPrendaID, p.ProcesoID, p.MuestraID, p.FechaCambio, p.Observaciones,
        c.ClienteID AS C_ClienteID, c.Nombre AS C_Nombre, c.Contacto AS C_Contacto, c.Estado AS C_Estado,
        m.MuestraID AS M_MuestraID, m.TipoPrenda AS M_TipoPrenda, m.PesoKilos AS M_PesoKilos, m.FechaCambio AS M_FechaCambio,
        em.EstadoMuestraID AS M_EstadoMuestraID, em.Estado AS M_EstadoNombre
FROM dbo.Prenda p
LEFT JOIN dbo.Cliente       c  ON c.ClienteID       = p.ClienteID
LEFT JOIN dbo.Muestra       m  ON m.MuestraID       = p.MuestraID
LEFT JOIN dbo.EstadoMuestra em ON em.EstadoMuestraID = m.EstadoMuestraID
WHERE p.PrendaID = @id;";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        int oPrendaID = rd.GetOrdinal("PrendaID");
        int oTipoPrenda = rd.GetOrdinal("TipoPrenda");
        int oNroCorte = rd.GetOrdinal("NroCorte");
        int oCantUnidades = rd.GetOrdinal("CantidadUnidades");
        int oPesoKilos = rd.GetOrdinal("PesoKilos");
        int oCantBolsas = rd.GetOrdinal("CantidadBolsas");
        int oFechaIngreso = rd.GetOrdinal("FechaIngreso");
        int oRemito = rd.GetOrdinal("Remito");
        int oEstadoPrendaID = rd.GetOrdinal("EstadoPrendaID");
        int oProcesoID = rd.GetOrdinal("ProcesoID");
        int oMuestraID = rd.GetOrdinal("MuestraID");
        int oFechaCambio = rd.GetOrdinal("FechaCambio");
        int oObservaciones = rd.GetOrdinal("Observaciones");

        int oC_ClienteID = rd.GetOrdinal("C_ClienteID");
        int oC_Nombre = rd.GetOrdinal("C_Nombre");
        int oC_Contacto = rd.GetOrdinal("C_Contacto");
        int oC_Estado = rd.GetOrdinal("C_Estado");

        int oM_MuestraID = rd.GetOrdinal("M_MuestraID");
        int oM_TipoPrenda = rd.GetOrdinal("M_TipoPrenda");
        int oM_PesoKilos = rd.GetOrdinal("M_PesoKilos");
        int oM_FechaCambio = rd.GetOrdinal("M_FechaCambio");
        int oM_EstadoMuestraID = rd.GetOrdinal("M_EstadoMuestraID");

        var dto = new Prenda
        {
            PrendaID = rd.GetInt32(oPrendaID),
            TipoPrenda = rd.IsDBNull(oTipoPrenda) ? null : rd.GetString(oTipoPrenda),
            NroCorte = rd.IsDBNull(oNroCorte) ? (int?)null : rd.GetInt32(oNroCorte),
            CantidadUnidades = rd.IsDBNull(oCantUnidades) ? (int?)null : rd.GetInt32(oCantUnidades),
            PesoKilos = rd.IsDBNull(oPesoKilos) ? (decimal?)null : rd.GetDecimal(oPesoKilos),
            CantidadBolsas = rd.IsDBNull(oCantBolsas) ? (int?)null : rd.GetInt32(oCantBolsas),
            FechaIngreso = rd.GetDateTime(oFechaIngreso),
            Remito = rd.IsDBNull(oRemito) ? null : rd.GetString(oRemito),
            EstadoPrendaID = rd.GetInt32(oEstadoPrendaID),
            ProcesoID = rd.GetInt32(oProcesoID),
            ClienteID = rd.IsDBNull(oC_ClienteID) ? 0 : rd.GetInt32(oC_ClienteID),
            MuestraID = rd.IsDBNull(oMuestraID) ? (int?)null : rd.GetInt32(oMuestraID),
            FechaCambio = rd.IsDBNull(oFechaCambio) ? (DateTime?)null : rd.GetDateTime(oFechaCambio),
            Observaciones = rd.IsDBNull(oObservaciones) ? null : rd.GetString(oObservaciones),
        };

        if (!rd.IsDBNull(oC_ClienteID))
        {
            dto.Cliente = new Client
            {
                ClientID = rd.GetInt32(oC_ClienteID),
                Username = rd.GetString(oC_Nombre),
                Contact = rd.IsDBNull(oC_Contacto) ? null : rd.GetString(oC_Contacto),
                State = rd.GetBoolean(oC_Estado)
            };
        }

        if (!rd.IsDBNull(oM_MuestraID))
        {
            dto.Muestra = new SampleDetailDTO
            {
                MuestraID = rd.GetInt32(oM_MuestraID),
                ClotheType = rd.IsDBNull(oM_TipoPrenda) ? null : rd.GetString(oM_TipoPrenda),
                KilogramWeight = rd.IsDBNull(oM_PesoKilos) ? 0 : rd.GetDecimal(oM_PesoKilos),
                ModifiedAt = rd.IsDBNull(oM_FechaCambio) ? (DateTime?)null : rd.GetDateTime(oM_FechaCambio),
                Status = new Models.SampleState.SampleStatus
                {
                    SampleStatusID = rd.IsDBNull(oM_EstadoMuestraID) ? 0 : rd.GetInt32(oM_EstadoMuestraID)
                }
            };
        }

        return dto;
    }


    public async Task<IEnumerable<EstadoPrenda>> GetEstadosPrendaAsync()
    {
        const string sql = "SELECT EstadoPrendaID, EstadoActual FROM dbo.EstadoPrendas ORDER BY EstadoPrendaID;";
        var list = new List<EstadoPrenda>();

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        await con.OpenAsync();

        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            list.Add(new EstadoPrenda
            {
                EstadoPrendaID = rd.GetInt32(rd.GetOrdinal("EstadoPrendaID")),
                EstadoActual = rd.GetString(rd.GetOrdinal("EstadoActual"))
            });

        return list;
    }

    public async Task<int> CreateEstadoPrendaAsync(EstadoPrendaCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.EstadoActual))
            throw new ArgumentException("EstadoActual es requerido.");

        // Generamos ID manualmente de forma segura y atómica
        const string getNextIdSql = @"
        SELECT ISNULL(MAX(EstadoPrendaID), 0) + 1
        FROM dbo.EstadoPrendas WITH (TABLOCKX, HOLDLOCK);";

        const string insertSql = @"
        INSERT INTO dbo.EstadoPrendas (EstadoPrendaID, EstadoActual)
        VALUES (@Id, @EstadoActual);";

        using var con = _db.GetConnection();
        await con.OpenAsync();

        // Serializable + TABLOCKX/HOLDLOCK evita IDs duplicados bajo concurrencia
        using var tx = con.BeginTransaction(System.Data.IsolationLevel.Serializable);

        try
        {
            int nextId;
            using (var getCmd = new SqlCommand(getNextIdSql, con, tx))
            {
                nextId = Convert.ToInt32(await getCmd.ExecuteScalarAsync());
            }

            using (var insCmd = new SqlCommand(insertSql, con, tx))
            {
                insCmd.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = nextId });
                insCmd.Parameters.Add(new SqlParameter("@EstadoActual", System.Data.SqlDbType.VarChar, 20) { Value = dto.EstadoActual });
                await insCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return nextId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // PrendaProceso: listar procesos de una prenda
    public async Task<IEnumerable<Process>> GetProcesosByPrendaIdAsync(int prendaId)
    {
        const string sql = @"
SELECT pp.PrendaProcesoID,
       p.ProcesoID, p.TipoProceso, p.Observaciones
FROM dbo.PrendaProceso pp
JOIN dbo.Proceso p ON p.ProcesoID = pp.ProcesoID
WHERE pp.PrendaID = @PrendaID
ORDER BY pp.PrendaProcesoID;";

        var list = new List<Process>();
        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@PrendaID", SqlDbType.Int) { Value = prendaId });

        await con.OpenAsync();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new Process
            {
                // ajustá los mapeos a tu clase Process / enum
                TypeProcess = rd.GetString(rd.GetOrdinal("TipoProceso")),
                Observations = rd.IsDBNull(rd.GetOrdinal("Observaciones")) ? null : rd.GetString(rd.GetOrdinal("Observaciones"))
            });
        }
        return list;
    }

    // PrendaProceso: agregar proceso a una prenda
    public async Task<int> AddProcesoToPrendaAsync(int prendaId, int procesoId)
    {
        const string existsSql = "SELECT COUNT(1) FROM dbo.PrendaProceso WHERE PrendaID=@p AND ProcesoID=@r;";
        const string insertSql = @"
    INSERT INTO dbo.PrendaProceso (PrendaID, ProcesoID)
    OUTPUT INSERTED.PrendaProcesoID
    VALUES (@p, @r);";

        using var con = _db.GetConnection();
        await con.OpenAsync();

        // prenda/proceso existen (ya lo hacías)
        // ...

        // evitar duplicado
        using (var e = new SqlCommand(existsSql, con))
        {
            e.Parameters.Add(new SqlParameter("@p", SqlDbType.Int) { Value = prendaId });
            e.Parameters.Add(new SqlParameter("@r", SqlDbType.Int) { Value = procesoId });
            if ((int)await e.ExecuteScalarAsync() > 0) return 0; // ya existe
        }

        using var cmd = new SqlCommand(insertSql, con);
        cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.Int) { Value = prendaId });
        cmd.Parameters.Add(new SqlParameter("@r", SqlDbType.Int) { Value = procesoId });

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public static async Task<bool> IsClientActiveAsync(SqlConnection con, SqlTransaction? tx, int clientId)
    {
        const string sql = "SELECT Estado FROM dbo.Cliente WHERE ClienteID = @id;";
        using var cmd = new SqlCommand(sql, con, tx);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = clientId });
        var val = await cmd.ExecuteScalarAsync();
        if (val is null) throw new InvalidOperationException($"Cliente no encontrado: {clientId}");
        return Convert.ToBoolean(val); // true = activo, false = inactivo
    }

    public async Task<bool> SetProcesoActualAsync(int prendaId, int procesoId)
    {
        const string sql = @"
    UPDATE dbo.Prenda
       SET ProcesoID = @ProcesoID, FechaCambio = GETDATE()
     WHERE PrendaID = @PrendaID;

    IF NOT EXISTS (SELECT 1 FROM dbo.PrendaProceso WHERE PrendaID = @PrendaID AND ProcesoID = @ProcesoID)
        INSERT INTO dbo.PrendaProceso (PrendaID, ProcesoID) VALUES (@PrendaID, @ProcesoID);

    SELECT @@ROWCOUNT; -- devuelve filas afectadas del UPDATE";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@PrendaID", SqlDbType.Int) { Value = prendaId });
        cmd.Parameters.Add(new SqlParameter("@ProcesoID", SqlDbType.Int) { Value = procesoId });

        await con.OpenAsync();
        var affected = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return affected > 0;
    }


}
