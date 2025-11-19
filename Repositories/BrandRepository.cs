using carrentalmvc.Data;
using carrentalmvc.Models;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class BrandRepository : Repository<Brand>, IBrandRepository
    {
        public BrandRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Brand?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(b => b.Name == name);
        }

        public async Task<IEnumerable<Brand>> GetActiveBrandsAsync()
        {
            return await _dbSet
                .Where(b => b.Cars.Any(c => c.IsActive))
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Brand>> GetBrandsWithCarsAsync()
        {
            return await _dbSet
                .Include(b => b.Cars)
                .Where(b => b.Cars.Any())
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<int> GetCarCountByBrandAsync(int brandId)
        {
            return await _context.Cars
                .CountAsync(c => c.BrandId == brandId && c.IsActive);
        }
    }
}