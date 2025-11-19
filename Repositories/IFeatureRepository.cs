using carrentalmvc.Models;

namespace carrentalmvc.Repositories
{
    public interface IFeatureRepository : IRepository<Feature>
    {
        Task<Feature?> GetByNameAsync(string name);
        Task<IEnumerable<Feature>> GetActiveeFeaturesAsync();
        Task<IEnumerable<Feature>> GetCarFeaturesAsync(int carId);
    }
}