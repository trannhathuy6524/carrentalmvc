using carrentalmvc.Models;

namespace carrentalmvc.Services
{
    public interface IFeatureService
    {
        Task<IEnumerable<Feature>> GetAllFeaturesAsync();
        Task<IEnumerable<Feature>> GetActiveFeaturesAsync();
        Task<Feature?> GetFeatureByIdAsync(int id);
        Task<Feature?> GetFeatureByNameAsync(string name);
        Task<Feature> CreateFeatureAsync(Feature feature);
        Task<Feature> UpdateFeatureAsync(Feature feature);
        Task<bool> DeleteFeatureAsync(int id);
        Task<bool> FeatureExistsAsync(int id);
        Task<bool> FeatureNameExistsAsync(string name, int? excludeId = null);
        Task<IEnumerable<Feature>> GetCarFeaturesAsync(int carId);
    }
}