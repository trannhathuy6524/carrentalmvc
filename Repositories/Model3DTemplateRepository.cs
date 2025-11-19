using carrentalmvc.Data;
using carrentalmvc.Models;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class Model3DTemplateRepository : Repository<Model3DTemplate>, IModel3DTemplateRepository
    {
        public Model3DTemplateRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Model3DTemplate>> GetAllWithIncludesAsync()
        {
            return await _context.Model3DTemplates
                .Include(t => t.Brand)
                .Include(t => t.Category)
                .ToListAsync();
        }

        public async Task<Model3DTemplate?> GetByIdWithIncludesAsync(int id)
        {
            return await _context.Model3DTemplates
                .Include(t => t.Brand)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.TemplateId == id);
        }

        public async Task<Model3DTemplate?> GetByNameAsync(string name)
        {
            return await _context.Model3DTemplates
                .FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<IEnumerable<Model3DTemplate>> GetActiveTemplatesAsync()
        {
            return await _context.Model3DTemplates
                .Include(t => t.Brand)
                .Include(t => t.Category)
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Model3DTemplate>> GetTemplatesByBrandAsync(int brandId)
        {
            return await _context.Model3DTemplates
                .Include(t => t.Brand)
                .Include(t => t.Category)
                .Where(t => t.IsActive && (t.BrandId == brandId || t.BrandId == null))
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Model3DTemplate>> GetTemplatesByCategoryAsync(int categoryId)
        {
            return await _context.Model3DTemplates
                .Include(t => t.Brand)
                .Include(t => t.Category)
                .Where(t => t.IsActive && (t.CategoryId == categoryId || t.CategoryId == null))
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<int> GetUsageCountAsync(int templateId)
        {
            return await _context.CarModels3D
                .CountAsync(cm => cm.TemplateId == templateId);
        }
    }
}