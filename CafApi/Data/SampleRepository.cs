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

        public SampleRepository(Db db)
        {
            _db = db;
        }

        public async Task<int> CreateAsync(CreateSampleDTO sample)
        {
            const string query = @"
    INSERT INTO dbo.Muestra (TipoPrenda, PesoKilos, ClienteID, EstadoMuestraID, FechaCambio)
    OUTPUT INSERTED.MuestraID
    VALUES (@TipoPrenda, @PesoKilos, @ClienteID, @EstadoMuestraID, @FechaCambio)";

            try
            {
                using var connection = _db.GetConnection();
                using var command = new SqlCommand(query, connection);
                await connection.OpenAsync();

                command.Parameters.AddWithValue("@TipoPrenda", sample.ClotheType);
                command.Parameters.AddWithValue("@PesoKilos", sample.KilogramWeight);
                command.Parameters.AddWithValue("@ClienteID", sample.ClientID);
                command.Parameters.AddWithValue("@EstadoMuestraID", sample.SampleStatusID);
                command.Parameters.Add(new SqlParameter("@FechaCambio", SqlDbType.DateTime) { Value = (object?)sample.ModifiedAt ?? DBNull.Value });

                return (int)await command.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error de base de datos al obtener Muestra", ex);
            }
        }

        public Task<int> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SampleDetailDTO>> GetAllAsync()
        {
            const string query = @"
                SELECT 
        m.TipoPrenda,
        m.PesoKilos,
        c.ClienteID,
        c.Contacto,
        c.Estado AS EstadoCliente,
        c.Nombre,
        em.EstadoMuestraID,
        em.Estado AS EstadoMuestra,
        mp.MuestraProcesoID,
        p.TipoProceso,
        p.Observaciones
    FROM Muestra as m
    INNER JOIN Cliente as c ON m.ClienteID = c.ClienteID
    INNER JOIN EstadoMuestra as em ON m.EstadoMuestraID = em.EstadoMuestraID
    LEFT JOIN MuestraProceso mp on m.MuestraID = mp.MuestraID
    LEFT JOIN Proceso p on mp.ProcesoID = p.ProcesoID";

            try
            {
                using var connection = _db.GetConnection();
                using var command = new SqlCommand(query, connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                List<SampleDetailDTO> samples = [];
                List<Process> processes = [];

                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("MuestraProcesoID")))
                    {
                        processes.Add(new Process()
                        {
                            Observations = reader.GetString(reader.GetOrdinal("Observaciones")) ?? null,
                            TypeProcess = (ETypeProcess)Enum.Parse(typeof(ETypeProcess), reader.GetString(reader.GetOrdinal("TipoProceso")))

                        });

                    }
                    samples.Add(new SampleDetailDTO
                    {
                        ClotheType = reader.GetString(reader.GetOrdinal("TipoPrenda")),
                        KilogramWeight = reader.GetDecimal(reader.GetOrdinal("PesoKilos")),
                        Client = new()
                        {
                            ClientID = reader.GetInt32(reader.GetOrdinal("ClienteID")),
                            Contact = reader.GetString(reader.GetOrdinal("Contacto")),
                            State = reader.GetBoolean(reader.GetOrdinal("EstadoCliente")),
                            Username = reader.GetString(reader.GetOrdinal("Nombre")),
                        },
                        Status = new()
                        {
                            SampleStatusID = reader.GetInt32(reader.GetOrdinal("EstadoMuestraID")),
                            Status = (ESampleStatus)Enum.Parse(typeof(ESampleStatus), reader.GetString(reader.GetOrdinal("EstadoMuestra")))
                        },
                       Processes = processes

                    });


                }
                return samples;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error de base de datos al obtener Muestra", ex);
            }
        }

        public async Task<SampleDetailDTO?> GetByIdAsync(int sampleId)
        {
            const string query = @"
                SELECT 
                m.TipoPrenda,
                m.PesoKilos,
                c.ClienteID,
                c.Contacto,
                c.Estado AS EstadoCliente,
                c.Nombre,
                em.EstadoMuestraID,
                em.Estado AS EstadoMuestra
                FROM Muestra as m
                INNER JOIN Cliente as c ON m.ClienteID = c.ClienteID
                INNER JOIN EstadoMuestra as em ON m.EstadoMuestraID = em.EstadoMuestraID
                where m.MuestraID = @SampleId";

            try
            {
                using var connection = _db.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SampleId", sampleId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                SampleDetailDTO sample = null;
                Client client = new() { };

                while (await reader.ReadAsync())
                {
                    if (sample is null)
                    {
                        sample = new()
                        {
                            ClotheType = reader.GetString(reader.GetOrdinal("TipoPrenda")),
                            KilogramWeight = reader.GetDecimal(reader.GetOrdinal("PesoKilos")),
                            Status = new()
                            {
                                SampleStatusID = reader.GetInt32(reader.GetOrdinal("EstadoMuestraID")),
                                Status = (ESampleStatus)Enum.Parse(typeof(ESampleStatus), reader.GetString(reader.GetOrdinal("EstadoMuestra")))
                            }
                        };
                    }

                    if (!reader.IsDBNull("ClienteID"))
                    {
                        client = new()
                        {
                            ClientID = reader.GetInt32(reader.GetOrdinal("ClienteID")),
                            Contact = reader.GetString(reader.GetOrdinal("Contacto")),
                            State = reader.GetBoolean(reader.GetOrdinal("EstadoCliente")),
                            Username = reader.GetString(reader.GetOrdinal("Nombre")),
                        };
                    }
                    if (sample is not null)
                    {
                        sample.Client = client;

                    }


                }
                return sample;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error de base de datos al obtener Muestra", ex);
            }
        }

        public async Task<int> UpdateAsync(int id, CreateSampleDTO sample)
        {
            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();

                const string query = @"
                    UPDATE [dbo].[Muestra]
                       SET TipoPrenda = @ClotheType
                          ,[PesoKilos] = @KilogramWeight
                          ,[ClienteID] = @ClientID
                          ,[EstadoMuestraID] = @SampleStatusID
                          ,[FechaCambio] = @ModifiedAt
                      WHERE MuestraID = @Id;";

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
            catch (Exception ex)
            {
                throw new Exception($"Error de base de datos al actualizar Muestra", ex);
            }
        }
    }
}
