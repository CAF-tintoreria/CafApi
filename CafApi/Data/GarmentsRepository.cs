using CafApi.Models;
using CafApi.Models.Sample;
using Microsoft.Data.SqlClient;
using System.Data;
using CafApi.Models.Sample;       // por SampleDetailDTO
using CafApi.Models.SampleState;   // por SampleStatus
using CafApi.Constants.Enum;

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
        while (await rd.ReadAsync())
        {
            list.Add(new Prenda
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
            });
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
        const string sql = @"
            INSERT INTO dbo.Prenda
            (TipoPrenda, NroCorte, CantidadUnidades, PesoKilos, CantidadBolsas,
             FechaIngreso, Remito, EstadoPrendaID, ProcesoID, ClienteID, MuestraID, Observaciones, FechaCambio)
            OUTPUT INSERTED.PrendaID
            VALUES
            (@TipoPrenda, @NroCorte, @CantidadUnidades, @PesoKilos, @CantidadBolsas,
             @FechaIngreso, @Remito, @EstadoPrendaID, @ProcesoID, @ClienteID, @MuestraID, @Observaciones, NULL)";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);

        cmd.Parameters.Add(new SqlParameter("@TipoPrenda", SqlDbType.NVarChar, 20) { Value = (object?)dto.TipoPrenda ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@NroCorte", SqlDbType.Int) { Value = (object?)dto.NroCorte ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CantidadUnidades", SqlDbType.Int) { Value = (object?)dto.CantidadUnidades ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@PesoKilos", SqlDbType.Decimal) { Value = (object?)dto.PesoKilos ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CantidadBolsas", SqlDbType.Int) { Value = (object?)dto.CantidadBolsas ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@FechaIngreso", SqlDbType.DateTime) { Value = dto.FechaIngreso });
        cmd.Parameters.Add(new SqlParameter("@Remito", SqlDbType.NVarChar, 20) { Value = (object?)dto.Remito ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@EstadoPrendaID", SqlDbType.Int) { Value = dto.EstadoPrendaID });
        cmd.Parameters.Add(new SqlParameter("@ProcesoID", SqlDbType.Int) { Value = dto.ProcesoID });
        cmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = dto.ClienteID });
        cmd.Parameters.Add(new SqlParameter("@MuestraID", SqlDbType.Int) { Value = (object?)dto.MuestraID ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Observaciones", SqlDbType.NVarChar, 200) { Value = (object?)dto.Observaciones ?? DBNull.Value });

        await con.OpenAsync();
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> UpdateAsync(int id, PrendaUpdateDto dto)
    {
        const string sql = @"
        UPDATE dbo.Prenda
        SET TipoPrenda        = @TipoPrenda,
            NroCorte          = @NroCorte,
            CantidadUnidades  = @CantidadUnidades,
            PesoKilos         = @PesoKilos,
            CantidadBolsas    = @CantidadBolsas,
            FechaIngreso      = @FechaIngreso,
            Remito            = @Remito,
            EstadoPrendaID    = @EstadoPrendaID,
            ProcesoID         = @ProcesoID,
            ClienteID         = @ClienteID,
            MuestraID         = @MuestraID,
            Observaciones     = @Observaciones,
            FechaCambio       = GETDATE()
        WHERE PrendaID = @Id;";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);

        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
        cmd.Parameters.Add(new SqlParameter("@TipoPrenda", SqlDbType.NVarChar, 20) { Value = (object?)dto.TipoPrenda ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@NroCorte", SqlDbType.Int) { Value = dto.NroCorte });
        cmd.Parameters.Add(new SqlParameter("@CantidadUnidades", SqlDbType.Int) { Value = dto.CantidadUnidades });

        // Precisión acorde a tu columna decimal(18,0)
        var pPeso = new SqlParameter("@PesoKilos", SqlDbType.Decimal) { Value = dto.PesoKilos, Precision = 18, Scale = 0 };
        cmd.Parameters.Add(pPeso);

        cmd.Parameters.Add(new SqlParameter("@CantidadBolsas", SqlDbType.Int) { Value = dto.CantidadBolsas });
        cmd.Parameters.Add(new SqlParameter("@FechaIngreso", SqlDbType.DateTime) { Value = dto.FechaIngreso });
        cmd.Parameters.Add(new SqlParameter("@Remito", SqlDbType.NVarChar, 20) { Value = (object?)dto.Remito ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@EstadoPrendaID", SqlDbType.Int) { Value = dto.EstadoPrendaID });
        cmd.Parameters.Add(new SqlParameter("@ProcesoID", SqlDbType.Int) { Value = dto.ProcesoID });
        cmd.Parameters.Add(new SqlParameter("@ClienteID", SqlDbType.Int) { Value = dto.ClienteID });
        cmd.Parameters.Add(new SqlParameter("@MuestraID", SqlDbType.Int) { Value = (object?)dto.MuestraID ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Observaciones", SqlDbType.NVarChar, 200) { Value = (object?)dto.Observaciones ?? DBNull.Value });

        await con.OpenAsync();
        return await cmd.ExecuteNonQueryAsync(); // filas afectadas (0 si no existe)
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
        const string sql = @"DELETE FROM dbo.Prenda WHERE PrendaID=@id";

        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

        await con.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<IEnumerable<Prenda>> GetAllWithClientAsync()
    {
        var list = new List<Prenda>();
        const string sql = @"
        SELECT  p.PrendaID, p.TipoPrenda, p.NroCorte, p.CantidadUnidades,
                p.PesoKilos, p.CantidadBolsas, p.FechaIngreso, p.Remito,
                p.EstadoPrendaID, p.ProcesoID, p.MuestraID, p.FechaCambio, p.Observaciones,
                c.ClienteID      AS C_ClienteID,
                c.Nombre         AS C_Nombre,
                c.Contacto       AS C_Contacto,
                c.Estado         AS C_Estado,
            m.MuestraID      AS M_MuestraID,
            m.TipoPrenda     AS M_TipoPrenda,
            m.PesoKilos      AS M_PesoKilos,
            m.FechaCambio    AS M_FechaCambio,
            em.EstadoMuestraID AS M_EstadoMuestraID,
            em.Estado        AS M_EstadoNombre
        FROM dbo.Prenda p
        LEFT JOIN dbo.Cliente c ON c.ClienteID = p.ClienteID
    LEFT JOIN dbo.Muestra       m  ON m.MuestraID       = p.MuestraID
    LEFT JOIN dbo.EstadoMuestra em ON em.EstadoMuestraID = m.EstadoMuestraID;";


        using var con = _db.GetConnection();
        using var cmd = new SqlCommand(sql, con);
        await con.OpenAsync();

        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            var dto = new Prenda
            {
                PrendaID = rd.GetInt32(rd.GetOrdinal("PrendaID")),
                TipoPrenda = rd.IsDBNull("TipoPrenda") ? null : rd.GetString("TipoPrenda"),
                NroCorte = rd.IsDBNull("NroCorte") ? (int?)null : rd.GetInt32("NroCorte"),
                CantidadUnidades = rd.IsDBNull("CantidadUnidades") ? (int?)null : rd.GetInt32("CantidadUnidades"),
                PesoKilos = rd.IsDBNull("PesoKilos") ? (decimal?)null : rd.GetDecimal("PesoKilos"),
                CantidadBolsas = rd.IsDBNull("CantidadBolsas") ? (int?)null : rd.GetInt32("CantidadBolsas"),
                FechaIngreso = rd.GetDateTime(rd.GetOrdinal("FechaIngreso")),
                Remito = rd.IsDBNull("Remito") ? null : rd.GetString("Remito"),
                EstadoPrendaID = rd.GetInt32(rd.GetOrdinal("EstadoPrendaID")),
                ProcesoID = rd.GetInt32(rd.GetOrdinal("ProcesoID")),
                ClienteID = rd.GetInt32("C_ClienteID"),
                MuestraID = rd.IsDBNull("MuestraID") ? (int?)null : rd.GetInt32("MuestraID"),
                FechaCambio = rd.IsDBNull("FechaCambio") ? (DateTime?)null : rd.GetDateTime("FechaCambio"),
                Observaciones = rd.IsDBNull("Observaciones") ? null : rd.GetString("Observaciones"),
            };

            // Cliente anidado (puede ser null si no hay coincidencia)
            if (!rd.IsDBNull(rd.GetOrdinal("C_ClienteID")))
            {
                dto.Cliente = new Client
                {
                    ClientID = rd.GetInt32(rd.GetOrdinal("C_ClienteID")),
                    Username = rd.GetString(rd.GetOrdinal("C_Nombre")),
                    Contact = rd.IsDBNull(rd.GetOrdinal("C_Contacto")) ? null : rd.GetString(rd.GetOrdinal("C_Contacto")),
                    State = rd.GetBoolean(rd.GetOrdinal("C_Estado"))
                };
            }

            if (!rd.IsDBNull("M_MuestraID"))
            {
                dto.Muestra = new SampleDetailDTO
                {
                    // Campos de SampleDTO base
                    ClotheType = rd.IsDBNull("M_TipoPrenda") ? null : rd.GetString("M_TipoPrenda"),
                    KilogramWeight = rd.IsDBNull("M_PesoKilos") ? 0 : rd.GetDecimal("M_PesoKilos"),
                    ModifiedAt = rd.IsDBNull("M_FechaCambio") ? (DateTime?)null : rd.GetDateTime("M_FechaCambio"),

                    // Cliente dentro de muestra (si en un futuro lo necesitás, ahora lo omitimos)
                    Client = null,

                    // Estado de la muestra con nombre
                    Status = new SampleStatus
                    {
                        SampleStatusID = rd.IsDBNull("M_EstadoMuestraID") ? 0 : rd.GetInt32("M_EstadoMuestraID")
                    }
                };
            }

            list.Add(dto);
        }

        return list;
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

        var dto = new Prenda
        {
            PrendaID = rd.GetInt32(rd.GetOrdinal("PrendaID")),
            TipoPrenda = rd.IsDBNull("TipoPrenda") ? null : rd.GetString("TipoPrenda"),
            NroCorte = rd.IsDBNull("NroCorte") ? (int?)null : rd.GetInt32("NroCorte"),
            CantidadUnidades = rd.IsDBNull("CantidadUnidades") ? (int?)null : rd.GetInt32("CantidadUnidades"),
            PesoKilos = rd.IsDBNull("PesoKilos") ? (decimal?)null : rd.GetDecimal("PesoKilos"),
            CantidadBolsas = rd.IsDBNull("CantidadBolsas") ? (int?)null : rd.GetInt32("CantidadBolsas"),
            FechaIngreso = rd.GetDateTime(rd.GetOrdinal("FechaIngreso")),
            Remito = rd.IsDBNull("Remito") ? null : rd.GetString("Remito"),
            EstadoPrendaID = rd.GetInt32(rd.GetOrdinal("EstadoPrendaID")),
            ProcesoID = rd.GetInt32(rd.GetOrdinal("ProcesoID")),
            ClienteID = rd.GetInt32("C_ClienteID"),
            MuestraID = rd.IsDBNull("MuestraID") ? (int?)null : rd.GetInt32("MuestraID"),
            FechaCambio = rd.IsDBNull("FechaCambio") ? (DateTime?)null : rd.GetDateTime("FechaCambio"),
            Observaciones = rd.IsDBNull("Observaciones") ? null : rd.GetString("Observaciones"),
        };

        if (!rd.IsDBNull(rd.GetOrdinal("C_ClienteID")))
        {
            dto.Cliente = new Client
            {
                ClientID = rd.GetInt32(rd.GetOrdinal("C_ClienteID")),
                Username = rd.GetString(rd.GetOrdinal("C_Nombre")),
                Contact = rd.IsDBNull(rd.GetOrdinal("C_Contacto")) ? null : rd.GetString(rd.GetOrdinal("C_Contacto")),
                State = rd.GetBoolean(rd.GetOrdinal("C_Estado"))
            };
        }

        if (!rd.IsDBNull("M_MuestraID"))
        {
            dto.Muestra = new SampleDetailDTO
            {
                // Campos de SampleDTO base
                ClotheType = rd.IsDBNull("M_TipoPrenda") ? null : rd.GetString("M_TipoPrenda"),
                KilogramWeight = rd.IsDBNull("M_PesoKilos") ? 0 : rd.GetDecimal("M_PesoKilos"),
                ModifiedAt = rd.IsDBNull("M_FechaCambio") ? (DateTime?)null : rd.GetDateTime("M_FechaCambio"),

                // Cliente dentro de muestra (si en un futuro lo necesitás, ahora lo omitimos)
                Client = null,

                // Estado de la muestra con nombre
                Status = new SampleStatus
                {
                    SampleStatusID = rd.IsDBNull("M_EstadoMuestraID") ? 0 : rd.GetInt32("M_EstadoMuestraID"),
                }
            };
        }

        return dto;
    }

}
