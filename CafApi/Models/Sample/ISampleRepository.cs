// Models/Sample/ISampleRepository.cs
namespace CafApi.Models.Sample
{
    public interface ISampleRepository
    {
        Task<SampleDetailDTO?> GetByIdAsync(int id);
        Task<IEnumerable<SampleDetailDTO>> GetAllAsync();

        // CRUD básicos
        Task<int> CreateAsync(CreateSampleDTO sample);
        Task<int> DeleteAsync(int id);
        Task<int> UpdateAsync(int id, CreateSampleDTO sample);

        // Navegación (igual filosofía que Prenda)
        Task<int> UpdateEstadoAsync(int sampleId, int nuevoEstadoId);
        Task<IEnumerable<Process>> GetProcesosBySampleIdAsync(int sampleId);
        Task<int> AddProcesoToSampleAsync(int sampleId, int procesoId);
    }
}
