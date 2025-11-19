using carrentalmvc.Models;
using carrentalmvc.Repositories;

namespace carrentalmvc.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _unitOfWork.Categories.GetAllAsync();
        }

        public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
        {
            return await _unitOfWork.Categories.GetActiveCategoriesAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _unitOfWork.Categories.GetByIdAsync(id);
        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            return await _unitOfWork.Categories.GetByNameAsync(name);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            try
            {
                if (await CategoryNameExistsAsync(category.Name))
                {
                    throw new InvalidOperationException($"Category với tên '{category.Name}' đã tồn tại.");
                }

                category.CreatedAt = DateTime.UtcNow;
                category.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã tạo category mới: {CategoryName} (ID: {CategoryId})", category.Name, category.CategoryId);
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo category: {CategoryName}", category.Name);
                throw;
            }
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            try
            {
                var existingCategory = await _unitOfWork.Categories.GetByIdAsync(category.CategoryId);
                if (existingCategory == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy category với ID: {category.CategoryId}");
                }

                if (await CategoryNameExistsAsync(category.Name, category.CategoryId))
                {
                    throw new InvalidOperationException($"Category với tên '{category.Name}' đã tồn tại.");
                }

                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;
                existingCategory.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Categories.Update(existingCategory);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã cập nhật category: {CategoryName} (ID: {CategoryId})", category.Name, category.CategoryId);
                return existingCategory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật category ID: {CategoryId}", category.CategoryId);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                {
                    return false;
                }

                var carCount = await GetCarCountByCategoryAsync(id);
                if (carCount > 0)
                {
                    throw new InvalidOperationException($"Không thể xóa category này vì có {carCount} xe đang sử dụng.");
                }

                _unitOfWork.Categories.Remove(category);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã xóa category: {CategoryName} (ID: {CategoryId})", category.Name, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa category ID: {CategoryId}", id);
                throw;
            }
        }

        public async Task<bool> CategoryExistsAsync(int id)
        {
            return await _unitOfWork.Categories.ExistsAsync(id);
        }

        public async Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null)
        {
            var existingCategory = await _unitOfWork.Categories.GetByNameAsync(name);
            if (existingCategory == null)
            {
                return false;
            }

            return excludeId == null || existingCategory.CategoryId != excludeId;
        }

        public async Task<int> GetCarCountByCategoryAsync(int categoryId)
        {
            return await _unitOfWork.Categories.GetCarCountByCategoryAsync(categoryId);
        }

        public async Task<IEnumerable<Category>> GetCategoriesWithCarsAsync()
        {
            return await _unitOfWork.Categories.GetCategoriesWithCarsAsync();
        }
    }
}