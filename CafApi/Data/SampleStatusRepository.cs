// Data/SampleStatusRepository.cs
using CafApi.Constants.Enum;
using CafApi.Models.SampleState;
using Microsoft.Data.SqlClient;

namespace CafApi.Data
{
    public class SampleStatusRepository : ISampleStatusRepository
    {
        private readonly Db _db;
        public SampleStatusRepository(Db db) => _db = db;

        // Igual que en SampleRepository
        private static string Normalize(string? s) => (s ?? string.Empty)
            .Trim()
            .Replace(" ", "")
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u");

        private static ESampleStatus ParseStatus(string? raw)
        {
            var normalized = Normalize(raw);
            return Enum.TryParse<ESampleStatus>(normalized, ignoreCase: true, out var val)
                ? val
                : default; // fallback seguro si no matchea
        }

        public async Task<ICollection<SampleStatus>> GetAllAsync()
        {
            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();

                const string query = @"SELECT EstadoMuestraID, Estado FROM dbo.EstadoMuestra";
                using var command = new SqlCommand(query, connection);

                var list = new List<SampleStatus>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new SampleStatus
                    {
                        SampleStatusID = reader.GetInt32(reader.GetOrdinal("EstadoMuestraID")),
                        Status = ParseStatus(reader.IsDBNull(reader.GetOrdinal("Estado"))
                                             ? null
                                             : reader.GetString(reader.GetOrdinal("Estado")))
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

                const string query = @"SELECT EstadoMuestraID, Estado
                                       FROM dbo.EstadoMuestra
                                       WHERE EstadoMuestraID = @SampleStateId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("SampleStateId", id);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;

                return new SampleStatus
                {
                    SampleStatusID = reader.GetInt32(reader.GetOrdinal("EstadoMuestraID")),
                    Status = ParseStatus(reader.IsDBNull(reader.GetOrdinal("Estado"))
                                         ? null
                                         : reader.GetString(reader.GetOrdinal("Estado")))
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error en la comunicación con la base de datos", ex);
            }
        }
    }
}
