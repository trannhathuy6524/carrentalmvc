using carrentalmvc.Models;

namespace carrentalmvc.Services
{
    public interface IBrandService
    {
        Task<IEnumerable<Brand>> GetAllBrandsAsync();
        Task<IEnumerable<Brand>> GetActiveBrandsAsync();
        Task<Brand?> GetBrandByIdAsync(int id);
        Task<Brand?> GetBrandByNameAsync(string name);
        Task<Brand> CreateBrandAsync(Brand brand);
        Task<Brand> UpdateBrandAsync(Brand brand);
        Task<bool> DeleteBrandAsync(int id);
        Task<bool> BrandExistsAsync(int id);
        Task<bool> BrandNameExistsAsync(string name, int? excludeId = null);
        Task<int> GetCarCountByBrandAsync(int brandId);
        Task<IEnumerable<Brand>> GetBrandsWithCarsAsync();
    }
}