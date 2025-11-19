using carrentalmvc.Models;

namespace carrentalmvc.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category?> GetByNameAsync(string name);
        Task<IEnumerable<Category>> GetActiveCategoriesAsync();
        Task<IEnumerable<Category>> GetCategoriesWithCarsAsync();
        Task<int> GetCarCountByCategoryAsync(int categoryId);
    }
}