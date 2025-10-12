using static CafApi.Models.Sample.SampleDTO;

namespace CafApi.Models.Sample
{
    public interface ISampleRepository
    {
        Task<SampleDetailDTO?> GetByIdAsync(int id);
        Task<IEnumerable<SampleDetailDTO>> GetAllAsync();
        Task<int> CreateAsync(CreateSampleDTO sample);
        Task<int> DeleteAsync(int id);
        Task<int> UpdateAsync(int id, CreateSampleDTO sample);
    }
}
