// Data/ProcessRepository.cs
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
            var sql = "SELECT 1 FROM dbo.Proceso WHERE TipoProceso = @tipo";
            if (excludeId.HasValue) sql += " AND ProcesoID <> @id";

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add(new SqlParameter("@tipo", SqlDbType.NVarChar, 80) { Value = typeProcess });
            if (excludeId.HasValue) cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = excludeId.Value });

            await con.OpenAsync();
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<int> CreateAsync(Process model)
        {
            try
            {

                const string sql = @"INSERT INTO dbo.Proceso (TipoProceso, Observaciones)
                                     OUTPUT INSERTED.ProcesoID
                                     VALUES (@tipo, @obs)";

                using var con = _db.GetConnection();
                using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.Add(new SqlParameter("@tipo", SqlDbType.NVarChar, 80) { Value = model.TypeProcess });
                cmd.Parameters.Add(new SqlParameter("@obs", SqlDbType.NVarChar, 300) { Value = (object?)model.Observations ?? DBNull.Value });

                await con.OpenAsync();
                var newId = (int)(await cmd.ExecuteScalarAsync()!);
                return newId;
            }
            catch(Exception ex)
            {
                throw new Exception("Ocurrio un error en la base de datos", ex);
            }
        }

        public async Task<bool> UpdateAsync(Process model)
        {
            try
            {
                const string sql = @"UPDATE dbo.Proceso
                                     SET  TipoProceso   = @tipo,
                                          Observaciones = @obs
                                     WHERE ProcesoID    = @id";

                using var con = _db.GetConnection();
                using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = model.ProcessID });
                cmd.Parameters.Add(new SqlParameter("@tipo", SqlDbType.NVarChar, 80) { Value = model.TypeProcess });
                cmd.Parameters.Add(new SqlParameter("@obs", SqlDbType.NVarChar, 300) { Value = (object?)model.Observations ?? DBNull.Value });

                await con.OpenAsync();
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;

            } catch (Exception ex)
            {
                throw new Exception("Ocurrio un error", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {

                const string sql = @"DELETE FROM dbo.Proceso WHERE ProcesoID = @id";

                using var con = _db.GetConnection();
                using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

                await con.OpenAsync();
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;

            }catch(Exception ex)
            {
                throw new Exception("Ocurrio un error en el proceso");
            }
        }
    }
}
