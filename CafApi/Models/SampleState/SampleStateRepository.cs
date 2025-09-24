using CafApi.Constants.Enum;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CafApi.Models.SampleState
{
    public class SampleStateRepository : ISampleStateRepository
    {
        private readonly Db _db;

        public SampleStateRepository(Db db)
        {
            _db = db;
        }

        public async Task<ICollection<SampleState>> GetAllAsync()
        {
            try
            {

                using var connection = _db.GetConnection();
                await connection.OpenAsync();

                const string query = @"SELECT EstadoMuestraID, Estado FROM dbo.EstadoMuestra";

                using var commmand = new SqlCommand(query, connection);

                var list = new List<SampleState>();

                using var reader = await commmand.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new()
                    {
                        SampleStateID = reader.GetInt32(reader.GetOrdinal("EstadoMuestraID")),
                        State = (ESampleState)Enum.Parse(typeof(ESampleState),reader.GetString(reader.GetOrdinal("Estado"))),
                    });
                }
                return list;

            }catch (Exception ex)
            {
                throw new Exception("Error en la comunicación con la base de datos", ex);
            }

        }

        public async Task<SampleState> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
