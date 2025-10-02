using CafApi.Constants.Enum;
using CafApi.Models.SampleState;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CafApi.Data
{
    public class SampleStatusRepository : ISampleStatusRepository
    {
        private readonly Db _db;

        public SampleStatusRepository(Db db)
        {
            _db = db;
        }

        public async Task<ICollection<SampleStatus>> GetAllAsync()
        {
            try
            {

                using var connection = _db.GetConnection();
                await connection.OpenAsync();

                const string query = @"SELECT EstadoMuestraID, Estado FROM dbo.EstadoMuestra";

                using var commmand = new SqlCommand(query, connection);

                var list = new List<SampleStatus>();

                using var reader = await commmand.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new()
                    {
                        SampleStatusID = reader.GetInt32(reader.GetOrdinal("EstadoMuestraID")),
                        Status = (ESampleStatus)Enum.Parse(typeof(ESampleStatus), reader.GetString(reader.GetOrdinal("Estado"))),
                    });
                }
                return list;

            }
            catch (Exception ex)
            {
                throw new Exception("Error en la comunicación con la base de datos", ex);
            }

        }

        public async Task<SampleStatus?> GetByIdAsync(int id)
        {
            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();

                const string query = @"SELECT EstadoMuestraID, Estado FROM dbo.EstadoMuestra 
                WHERE EstadoMuestraID = @SampleStateId";

                using var commmand = new SqlCommand(query, connection);

                commmand.Parameters.AddWithValue("SampleStateId", id);

                SampleStatus sampleState;

                using var reader = await commmand.ExecuteReaderAsync();

                if(await reader.ReadAsync())
                {
                    sampleState = new()
                    {
                        SampleStatusID = reader.GetInt32(reader.GetOrdinal("EstadoMuestraID")),
                        Status = (ESampleStatus)Enum.Parse(typeof(ESampleStatus), reader.GetString(reader.GetOrdinal("Estado")))
                    };
                } else
                {
                    return null;
                }

                return sampleState;

            }catch (Exception ex)
            {
                throw new Exception("Error en la comunicación con la base de datos", ex);
            }
        }
    }
}
