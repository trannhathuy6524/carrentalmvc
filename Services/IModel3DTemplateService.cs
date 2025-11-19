using carrentalmvc.Models;

namespace carrentalmvc.Services
{
    public interface IModel3DTemplateService
    {
        Task<IEnumerable<Model3DTemplate>> GetAllTemplatesAsync();
        Task<IEnumerable<Model3DTemplate>> GetActiveTemplatesAsync();
        Task<IEnumerable<Model3DTemplate>> GetTemplatesByBrandAsync(int brandId);
        Task<IEnumerable<Model3DTemplate>> GetTemplatesByCategoryAsync(int categoryId);
        Task<Model3DTemplate?> GetTemplateByIdAsync(int id);
        Task<Model3DTemplate> CreateTemplateAsync(Model3DTemplate template);
        Task<Model3DTemplate> UpdateTemplateAsync(Model3DTemplate template);
        Task<bool> DeleteTemplateAsync(int id);
        Task<bool> ToggleActiveStatusAsync(int id);
        Task<int> GetUsageCountAsync(int templateId);
    }
}