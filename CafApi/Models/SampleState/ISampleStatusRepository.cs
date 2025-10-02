namespace CafApi.Models.SampleState
{
    public interface ISampleStatusRepository
    {
        public Task<ICollection<SampleStatus>> GetAllAsync();
        public Task<SampleStatus?> GetByIdAsync(int id);
    }
}
