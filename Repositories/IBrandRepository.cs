using carrentalmvc.Models;

namespace carrentalmvc.Repositories
{
    public interface IBrandRepository : IRepository<Brand>
    {
        Task<Brand?> GetByNameAsync(string name);
        Task<IEnumerable<Brand>> GetActiveBrandsAsync();
        Task<IEnumerable<Brand>> GetBrandsWithCarsAsync();
        Task<int> GetCarCountByBrandAsync(int brandId);
    }
}