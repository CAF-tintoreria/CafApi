namespace CafApi.Models.SampleState
{
    public interface ISampleStateRepository
    {
        public Task<ICollection<SampleState>> GetAllAsync();
        public Task<SampleState> GetByIdAsync(int id);
    }
}
