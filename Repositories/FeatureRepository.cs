using carrentalmvc.Data;
using carrentalmvc.Models;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class FeatureRepository : Repository<Feature>, IFeatureRepository
    {
        public FeatureRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Feature?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(f => f.Name == name);
        }

        public async Task<IEnumerable<Feature>> GetActiveeFeaturesAsync()
        {
            return await _dbSet
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feature>> GetCarFeaturesAsync(int carId)
        {
            return await _context.CarFeatures
                .Where(cf => cf.CarId == carId)
                .Include(cf => cf.Feature)
                .Select(cf => cf.Feature)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
    }
}