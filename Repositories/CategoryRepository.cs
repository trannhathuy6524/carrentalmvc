using carrentalmvc.Data;
using carrentalmvc.Models;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Name == name);
        }

        public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
        {
            return await _dbSet
                .Where(c => c.Cars.Any(car => car.IsActive))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetCategoriesWithCarsAsync()
        {
            return await _dbSet
                .Include(c => c.Cars)
                .Where(c => c.Cars.Any())
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<int> GetCarCountByCategoryAsync(int categoryId)
        {
            return await _context.Cars
                .CountAsync(c => c.CategoryId == categoryId && c.IsActive);
        }
    }
}