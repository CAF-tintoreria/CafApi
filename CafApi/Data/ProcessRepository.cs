// Data/ProcessRepository.cs
using CafApi.Constants.Enum;
using CafApi.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CafApi.Data
{
    public class ProcessRepository
    {
        private readonly Db _db;

        public ProcessRepository(Db db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Process>> GetAllAsync()
        {
            var list = new List<Process>();
            const string sql = @"SELECT ProcesoID, TipoProceso, Observaciones FROM dbo.Proceso";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);

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

        public async Task<Process?> GetByIdAsync(int id)
        {
            const string sql = @"SELECT ProcesoID, TipoProceso, Observaciones
                                 FROM dbo.Proceso
                                 WHERE ProcesoID = @id";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return null;

            return new Process
            {
                ProcessID = rd.GetInt32(rd.GetOrdinal("ProcesoID")),
                TypeProcess = rd.GetString(rd.GetOrdinal("TipoProceso")),
                Observations = rd.IsDBNull(rd.GetOrdinal("Observaciones")) ? null : rd.GetString(rd.GetOrdinal("Observaciones"))
            };
        }

        public async Task<bool> ExistsByNameAsync(string typeProcess, int? excludeId = null)
        {
            var sql = "SELECT 1 FROM dbo.Proceso WHERE UPPER(TipoProceso) = UPPER(@tipo)";
            if (excludeId.HasValue) sql += " AND ProcesoID <> @id";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@tipo", SqlDbType.NVarChar, 50) { Value = typeProcess });
            if (excludeId.HasValue) cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = excludeId.Value });

            await con.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<int> CreateAsync(Process model)
        {
            const string sql = @"INSERT INTO dbo.Proceso (TipoProceso, Observaciones)
                         OUTPUT INSERTED.ProcesoID
                         VALUES (@tipo, @obs)";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@tipo", SqlDbType.NVarChar, 50) { Value = model.TypeProcess });
            cmd.Parameters.Add(new SqlParameter("@obs", SqlDbType.NVarChar, 250) { Value = (object?)model.Observations ?? DBNull.Value });

            await con.OpenAsync();
            var newId = (int)(await cmd.ExecuteScalarAsync()!);
            return newId;
        }

        public async Task<bool> UpdateAsync(Process model)
        {
            const string sql = @"UPDATE dbo.Proceso
                         SET  TipoProceso   = @tipo,
                              Observaciones = @obs
                         WHERE ProcesoID    = @id";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = model.ProcessID });
            cmd.Parameters.Add(new SqlParameter("@tipo", SqlDbType.NVarChar, 50) { Value = model.TypeProcess });
            cmd.Parameters.Add(new SqlParameter("@obs", SqlDbType.NVarChar, 250) { Value = (object?)model.Observations ?? DBNull.Value });

            await con.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }


        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = @"DELETE FROM dbo.Proceso WHERE ProcesoID = @id";
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
            await con.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> HasRelationsAsync(int id)
        {
            const string sql = @"
        SELECT 1
        WHERE EXISTS(SELECT 1 FROM dbo.PrendaProceso  WHERE ProcesoID = @id)
           OR EXISTS(SELECT 1 FROM dbo.MuestraProceso WHERE ProcesoID = @id);";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
            await con.OpenAsync();
            var r = await cmd.ExecuteScalarAsync();
            return r is not null;
        }

        public async Task<(int muestras, int prendas)> GetReferenceCountsAsync(int processId)
        {
            const string sql = @"
        SELECT 
            (SELECT COUNT(*) FROM dbo.MuestraProceso WHERE ProcesoID = @id) AS Muestras,
            (SELECT COUNT(*) FROM dbo.PrendaProceso  WHERE ProcesoID = @id) AS Prendas;";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = processId });

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();
            await rd.ReadAsync();

            var muestras = rd.GetInt32(0);
            var prendas = rd.GetInt32(1);
            return (muestras, prendas);
        }

    

    public async Task<bool> RemoveProcesoFromPrendaAsync(int prendaId, int procesoId)
        {
            const string checkActual = "SELECT ProcesoID FROM dbo.Prenda WHERE PrendaID=@p;";
            const string deleteSql = "DELETE FROM dbo.PrendaProceso WHERE PrendaID=@p AND ProcesoID=@r;";

            using var con = _db.GetConnection();
            await con.OpenAsync();

            int actual;
            using (var c = new SqlCommand(checkActual, con))
            {
                c.Parameters.Add(new SqlParameter("@p", SqlDbType.Int) { Value = prendaId });
                var val = await c.ExecuteScalarAsync();
                if (val is null) return false; // prenda no existe
                actual = Convert.ToInt32(val);
            }

            if (actual == procesoId)
                throw new InvalidOperationException("No se puede quitar el proceso actual de la prenda.");

            using var del = new SqlCommand(deleteSql, con);
            del.Parameters.Add(new SqlParameter("@p", SqlDbType.Int) { Value = prendaId });
            del.Parameters.Add(new SqlParameter("@r", SqlDbType.Int) { Value = procesoId });
            return await del.ExecuteNonQueryAsync() > 0;
        }

    }
    }

