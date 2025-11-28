// Data/SampleRepository.cs
using CafApi.Constants.Enum;
using CafApi.Models;
using CafApi.Models.Sample;
using Microsoft.Data.SqlClient;
using System.Data;
using static CafApi.Models.Sample.SampleDTO;

namespace CafApi.Data
{
    public class SampleRepository : ISampleRepository
    {
        private readonly Db _db;
        public SampleRepository(Db db) => _db = db;

        public async Task<int> CreateAsync(CreateSampleDTO sample)
        {
            const string query = @"
INSERT INTO dbo.Muestra (TipoPrenda, PesoKilos, ClienteID, EstadoMuestraID, FechaCambio)
OUTPUT INSERTED.MuestraID
VALUES (@TipoPrenda, @PesoKilos, @ClienteID, @EstadoMuestraID, @FechaCambio);";

            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            if (!await IsClientActiveAsync(connection, null, sample.ClientID))
                throw new InvalidOperationException("El cliente seleccionado está inactivo. No se puede asignar una muestra.");

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TipoPrenda", sample.ClotheType);
            command.Parameters.AddWithValue("@PesoKilos", sample.KilogramWeight);
            command.Parameters.AddWithValue("@ClienteID", sample.ClientID);
            command.Parameters.AddWithValue("@EstadoMuestraID", sample.SampleStatusID);
            command.Parameters.Add(new SqlParameter("@FechaCambio", SqlDbType.DateTime) { Value = (object?)sample.ModifiedAt ?? DBNull.Value });

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }


        public async Task<int> UpdateAsync(int id, CreateSampleDTO sample)
        {
            const string query = @"
UPDATE dbo.Muestra
   SET TipoPrenda = @ClotheType,
       PesoKilos = @KilogramWeight,
       ClienteID = @ClientID,
       EstadoMuestraID = @SampleStatusID,
       FechaCambio = @ModifiedAt
 WHERE MuestraID = @Id;";

            using var connection = _db.GetConnection();
            await connection.OpenAsync();

            // 1) Unicidad de nombre (no puede existir en otra muestra)
            if (await ExistsClotheTypeAsync(connection, null, sample.ClotheType, excludeId: id))
                throw new InvalidOperationException("Ya existe otra muestra con ese nombre de prenda (ClotheType).");

            // 2) Cliente activo
            if (!await IsClientActiveAsync(connection, null, sample.ClientID))
                throw new InvalidOperationException("El cliente seleccionado está inactivo. No se puede asignar una muestra.");

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ClotheType", sample.ClotheType);
            command.Parameters.AddWithValue("@KilogramWeight", sample.KilogramWeight);
            command.Parameters.AddWithValue("@ClientID", sample.ClientID);
            command.Parameters.AddWithValue("@SampleStatusID", sample.SampleStatusID);
            command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0 ? id : -1;
        }

        public async Task<int> DeleteAsync(int id)
        {
            using var con = _db.GetConnection();
            await con.OpenAsync();

            using var tx = con.BeginTransaction();

            try
            {
                // A) Si hay prendas asociadas NO terminadas => bloquear
                var notFinished = await CountLinkedGarmentsNotFinishedAsync(con, tx, id);
                if (notFinished > 0)
                    throw new InvalidOperationException("No se puede eliminar la muestra: hay prendas asociadas que no están en estado 'Terminado'.");

                // B) Desvincular prendas 'Terminado' antes de borrar (para no romper FK)
                await UnlinkFinishedGarmentsAsync(con, tx, id);

                // C) Borrar la muestra
                int rows;
                using (var del = new SqlCommand("DELETE FROM dbo.Muestra WHERE MuestraID = @id;", con, tx))
                {
                    del.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
                    rows = await del.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return rows; // 1 si borró, 0 si no existe
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ======= NAVEGACIÓN =======
        public async Task<IEnumerable<SampleDetailDTO>> GetAllAsync()
        {
            const string query = @"
SELECT  m.MuestraID, m.TipoPrenda, m.PesoKilos, m.FechaCambio,

        c.ClienteID      AS C_ClienteID,
        c.Nombre         AS C_Nombre,
        c.Contacto       AS C_Contacto,
        c.Estado         AS C_Estado,

        em.EstadoMuestraID AS EM_EstadoMuestraID,
        em.Estado          AS EM_EstadoNombre,

        mp.MuestraProcesoID,
        p.ProcesoID       AS PR_ProcesoID,
        p.TipoProceso     AS PR_TipoProceso,
        p.Observaciones   AS PR_Observaciones

FROM dbo.Muestra m
LEFT JOIN dbo.Cliente        c  ON c.ClienteID        = m.ClienteID
LEFT JOIN dbo.EstadoMuestra  em ON em.EstadoMuestraID = m.EstadoMuestraID
LEFT JOIN dbo.MuestraProceso mp ON mp.MuestraID       = m.MuestraID
LEFT JOIN dbo.Proceso        p  ON p.ProcesoID        = mp.ProcesoID
ORDER BY m.MuestraID, mp.MuestraProcesoID;";

            // normalizador local (acentos/espacios) para mapear al nombre del enum
            static string Normalize(string? s) => (s ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u");

            var byId = new Dictionary<int, SampleDetailDTO>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(query, con);
            await con.OpenAsync();

            using var rd = await cmd.ExecuteReaderAsync();

            int oMuestraID = rd.GetOrdinal("MuestraID");
            int oTipoPrenda = rd.GetOrdinal("TipoPrenda");
            int oPesoKilos = rd.GetOrdinal("PesoKilos");
            int oFechaCambio = rd.GetOrdinal("FechaCambio");

            int oC_ClienteID = rd.GetOrdinal("C_ClienteID");
            int oC_Nombre = rd.GetOrdinal("C_Nombre");
            int oC_Contacto = rd.GetOrdinal("C_Contacto");
            int oC_Estado = rd.GetOrdinal("C_Estado");

            int oEM_EstadoMuestraID = rd.GetOrdinal("EM_EstadoMuestraID");
            int oEM_EstadoNombre = rd.GetOrdinal("EM_EstadoNombre");

            int oPR_ProcesoID = rd.GetOrdinal("PR_ProcesoID");
            int oPR_TipoProceso = rd.GetOrdinal("PR_TipoProceso");
            int oPR_Observaciones = rd.GetOrdinal("PR_Observaciones");

            while (await rd.ReadAsync())
            {
                var id = rd.GetInt32(oMuestraID);

                if (!byId.TryGetValue(id, out var dto))
                {
                    // Parseo robusto del estado
                    var rawStatus = rd.IsDBNull(oEM_EstadoNombre) ? null : rd.GetString(oEM_EstadoNombre);
                    var normalized = Normalize(rawStatus);
                    if (!Enum.TryParse<ESampleStatus>(normalized, ignoreCase: true, out var statusEnum))
                        statusEnum = default; // fallback al primer valor del enum

                    dto = new SampleDetailDTO
                    {
                        MuestraID = id,
                        ClotheType = rd.GetString(oTipoPrenda),
                        KilogramWeight = rd.GetDecimal(oPesoKilos),
                        ModifiedAt = rd.IsDBNull(oFechaCambio) ? (DateTime?)null : rd.GetDateTime(oFechaCambio),

                        Client = !rd.IsDBNull(oC_ClienteID) ? new Client
                        {
                            ClientID = rd.GetInt32(oC_ClienteID),
                            Username = rd.GetString(oC_Nombre),
                            Contact = rd.IsDBNull(oC_Contacto) ? null : rd.GetString(oC_Contacto),
                            State = rd.GetBoolean(oC_Estado)
                        } : null!,

                        Status = new Models.SampleState.SampleStatus
                        {
                            SampleStatusID = rd.IsDBNull(oEM_EstadoMuestraID) ? 0 : rd.GetInt32(oEM_EstadoMuestraID),
                            Status = statusEnum
                        },

                        Processes = new List<Process>()
                    };

                    byId.Add(id, dto);
                }

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

        public async Task<SampleDetailDTO?> GetByIdAsync(int sampleId)
        {
            const string query = @"
SELECT  m.MuestraID, m.TipoPrenda, m.PesoKilos, m.FechaCambio,

        c.ClienteID      AS C_ClienteID,
        c.Nombre         AS C_Nombre,
        c.Contacto       AS C_Contacto,
        c.Estado         AS C_Estado,

        em.EstadoMuestraID AS EM_EstadoMuestraID,
        em.Estado          AS EM_EstadoNombre,

        mp.MuestraProcesoID,
        p.ProcesoID       AS PR_ProcesoID,
        p.TipoProceso     AS PR_TipoProceso,
        p.Observaciones   AS PR_Observaciones

FROM dbo.Muestra m
LEFT JOIN dbo.Cliente        c  ON c.ClienteID        = m.ClienteID
LEFT JOIN dbo.EstadoMuestra  em ON em.EstadoMuestraID = m.EstadoMuestraID
LEFT JOIN dbo.MuestraProceso mp ON mp.MuestraID       = m.MuestraID
LEFT JOIN dbo.Proceso        p  ON p.ProcesoID        = mp.ProcesoID
WHERE m.MuestraID = @id
ORDER BY mp.MuestraProcesoID;";

            static string Normalize(string? s) => (s ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u");

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", sampleId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            SampleDetailDTO? dto = null;

            int oMuestraID = rd.GetOrdinal("MuestraID");
            int oTipoPrenda = rd.GetOrdinal("TipoPrenda");
            int oPesoKilos = rd.GetOrdinal("PesoKilos");
            int oFechaCambio = rd.GetOrdinal("FechaCambio");

            int oC_ClienteID = rd.GetOrdinal("C_ClienteID");
            int oC_Nombre = rd.GetOrdinal("C_Nombre");
            int oC_Contacto = rd.GetOrdinal("C_Contacto");
            int oC_Estado = rd.GetOrdinal("C_Estado");

            int oEM_EstadoMuestraID = rd.GetOrdinal("EM_EstadoMuestraID");
            int oEM_EstadoNombre = rd.GetOrdinal("EM_EstadoNombre");

            int oPR_ProcesoID = rd.GetOrdinal("PR_ProcesoID");
            int oPR_TipoProceso = rd.GetOrdinal("PR_TipoProceso");
            int oPR_Observaciones = rd.GetOrdinal("PR_Observaciones");

            while (await rd.ReadAsync())
            {
                if (dto is null)
                {
                    // Parseo robusto del estado
                    var rawStatus = rd.IsDBNull(oEM_EstadoNombre) ? null : rd.GetString(oEM_EstadoNombre);
                    var normalized = Normalize(rawStatus);
                    if (!Enum.TryParse<ESampleStatus>(normalized, ignoreCase: true, out var statusEnum))
                        statusEnum = default;

                    dto = new SampleDetailDTO
                    {
                        MuestraID = rd.GetInt32(oMuestraID),
                        ClotheType = rd.GetString(oTipoPrenda),
                        KilogramWeight = rd.GetDecimal(oPesoKilos),
                        ModifiedAt = rd.IsDBNull(oFechaCambio) ? (DateTime?)null : rd.GetDateTime(oFechaCambio),

                        Client = !rd.IsDBNull(oC_ClienteID) ? new Client
                        {
                            ClientID = rd.GetInt32(oC_ClienteID),
                            Username = rd.GetString(oC_Nombre),
                            Contact = rd.IsDBNull(oC_Contacto) ? null : rd.GetString(oC_Contacto),
                            State = rd.GetBoolean(oC_Estado)
                        } : null!,

                        Status = new Models.SampleState.SampleStatus
                        {
                            SampleStatusID = rd.IsDBNull(oEM_EstadoMuestraID) ? 0 : rd.GetInt32(oEM_EstadoMuestraID),
                            Status = statusEnum
                        },

                        Processes = new List<Process>()
                    };
                }

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

            return dto;
        }

        public async Task<int> UpdateEstadoAsync(int sampleId, int nuevoEstadoId)
        {
            const string sql = @"
UPDATE dbo.Muestra
   SET EstadoMuestraID = @EstadoMuestraID,
       FechaCambio     = GETDATE()
 WHERE MuestraID = @MuestraID
   AND EstadoMuestraID <> @EstadoMuestraID;

IF @@ROWCOUNT = 0
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.Muestra WHERE MuestraID = @MuestraID)
        SELECT 0;       -- sin cambio
    ELSE
        SELECT -1;      -- no encontrado
END
ELSE
    SELECT 1;           -- actualizado
";
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@MuestraID", SqlDbType.Int) { Value = sampleId });
            cmd.Parameters.Add(new SqlParameter("@EstadoMuestraID", SqlDbType.Int) { Value = nuevoEstadoId });

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<IEnumerable<Process>> GetProcesosBySampleIdAsync(int sampleId)
        {
            const string sql = @"
SELECT mp.MuestraProcesoID,
       p.ProcesoID,
       p.TipoProceso,
       p.Observaciones
FROM dbo.MuestraProceso mp
JOIN dbo.Proceso p ON p.ProcesoID = mp.ProcesoID
WHERE mp.MuestraID = @MuestraID
ORDER BY mp.MuestraProcesoID;";

            var list = new List<Process>();
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@MuestraID", SqlDbType.Int) { Value = sampleId });

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new Process
                {
                    ProcessID = rd.GetInt32(rd.GetOrdinal("ProcesoID")),
                    TypeProcess = rd.GetString(rd.GetOrdinal("TipoProceso")),
                    Observations = rd.IsDBNull(rd.GetOrdinal("Observaciones")) ? null : rd.GetString(rd.GetOrdinal("Observaciones"))
                });
            }
            return list;
        }

        public async Task<int> AddProcesoToSampleAsync(int sampleId, int procesoId)
        {
            const string existsSql = "SELECT COUNT(1) FROM dbo.MuestraProceso WHERE MuestraID=@m AND ProcesoID=@p;";
            const string insertSql = @"
INSERT INTO dbo.MuestraProceso (MuestraID, ProcesoID)
OUTPUT INSERTED.MuestraProcesoID
VALUES (@m, @p);";

            using var con = _db.GetConnection();
            await con.OpenAsync();

            using (var e = new SqlCommand(existsSql, con))
            {
                e.Parameters.Add(new SqlParameter("@m", SqlDbType.Int) { Value = sampleId });
                e.Parameters.Add(new SqlParameter("@p", SqlDbType.Int) { Value = procesoId });
                if ((int)await e.ExecuteScalarAsync() > 0) return 0; // ya existe
            }

            using var cmd = new SqlCommand(insertSql, con);
            cmd.Parameters.Add(new SqlParameter("@m", SqlDbType.Int) { Value = sampleId });
            cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.Int) { Value = procesoId });

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private static async Task<bool> IsClientActiveAsync(SqlConnection con, SqlTransaction? tx, int clientId)
        {
            const string sql = "SELECT Estado FROM dbo.Cliente WHERE ClienteID = @id;";
            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@id", clientId);
            var val = await cmd.ExecuteScalarAsync();
            if (val is null) throw new InvalidOperationException($"Cliente no encontrado: {clientId}");
            return Convert.ToBoolean(val);
        }


        private static async Task<bool> ExistsClotheTypeAsync(
    SqlConnection con, SqlTransaction? tx, string clotheType, int? excludeId = null)
        {
            // Comparación case/acentos-insensible (ajustá el COLLATE si usás otro)
            var sql = @"
SELECT COUNT(1)
FROM dbo.Muestra
WHERE TipoPrenda COLLATE Latin1_General_CI_AI = @tipo
" + (excludeId.HasValue ? "AND MuestraID <> @id" : "");

            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.AddWithValue("@tipo", clotheType);
            if (excludeId.HasValue)
                cmd.Parameters.AddWithValue("@id", excludeId.Value);

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        private static async Task<int> CountLinkedGarmentsNotFinishedAsync(
            SqlConnection con, SqlTransaction tx, int sampleId)
        {
            // Cuenta prendas vinculadas a la muestra cuyo estado NO es 'Terminado'
            const string sql = @"
SELECT COUNT(1)
FROM dbo.Prenda p
JOIN dbo.EstadoPrendas e ON e.EstadoPrendaID = p.EstadoPrendaID
WHERE p.MuestraID = @id
  AND e.EstadoActual <> 'Terminado';";

            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = sampleId });
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private static async Task<int> UnlinkFinishedGarmentsAsync(
            SqlConnection con, SqlTransaction tx, int sampleId)
        {
            // Desvincula (pone NULL) las prendas asociadas en estado 'Terminado'
            const string sql = @"
UPDATE p
   SET MuestraID = NULL
FROM dbo.Prenda p
JOIN dbo.EstadoPrendas e ON e.EstadoPrendaID = p.EstadoPrendaID
WHERE p.MuestraID = @id
  AND e.EstadoActual = 'Terminado';";

            using var cmd = new SqlCommand(sql, con, tx);
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = sampleId });
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
