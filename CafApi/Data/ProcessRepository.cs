using CafApi.Models;
using Microsoft.Data.SqlClient;
using System.Buffers;
using System.Data;

namespace CafApi.Data
{
    public class ProcessRepository
    {
        private readonly Db _db;
        public async Task<IEnumerable<Process>> GetAllAsync() 
        { 
            var list = new List<Process>();
            var sql = @"SELECT ProcessID, TipoProceso, Observaciones
                    FROM dbo.Proceso";

            try
            {

                using var con = _db.GetConnection();
                using var cmd = new SqlCommand(sql, con);

                await con.OpenAsync();
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    list.Add(new Process
                    {
                        TypeProcess = rd.GetEnumerator(rd.GetOrdinal("TipoProceso")),
                        ProcessID = rd.GetInt32(rd.GetOrdinal("Idproceso")),
                        Observations = rd.GetString(rd.GetOrdinal("Observaciones"))

                    });



                }


                return list;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error de base de datos al obtener proceso", ex);
            }
        }

    }
} 
 