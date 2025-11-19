using carrentalmvc.Models;

namespace carrentalmvc.Repositories
{
    public interface IModel3DTemplateRepository : IRepository<Model3DTemplate>
    {
        Task<IEnumerable<Model3DTemplate>> GetAllWithIncludesAsync();
        Task<Model3DTemplate?> GetByIdWithIncludesAsync(int id);
        Task<Model3DTemplate?> GetByNameAsync(string name);
        Task<IEnumerable<Model3DTemplate>> GetActiveTemplatesAsync();
        Task<IEnumerable<Model3DTemplate>> GetTemplatesByBrandAsync(int brandId);
        Task<IEnumerable<Model3DTemplate>> GetTemplatesByCategoryAsync(int categoryId);
        Task<int> GetUsageCountAsync(int templateId);
    }
}