using carrentalmvc.Models;
using carrentalmvc.Repositories;

namespace carrentalmvc.Services
{
    public class BrandService : IBrandService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BrandService> _logger;

        public BrandService(IUnitOfWork unitOfWork, ILogger<BrandService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Brand>> GetAllBrandsAsync()
        {
            return await _unitOfWork.Brands.GetAllAsync();
        }

        public async Task<IEnumerable<Brand>> GetActiveBrandsAsync()
        {
            return await _unitOfWork.Brands.GetActiveBrandsAsync();
        }

        public async Task<Brand?> GetBrandByIdAsync(int id)
        {
            return await _unitOfWork.Brands.GetByIdAsync(id);
        }

        public async Task<Brand?> GetBrandByNameAsync(string name)
        {
            return await _unitOfWork.Brands.GetByNameAsync(name);
        }

        public async Task<Brand> CreateBrandAsync(Brand brand)
        {
            try
            {
                // Kiểm tra trùng tên
                if (await BrandNameExistsAsync(brand.Name))
                {
                    throw new InvalidOperationException($"Brand với tên '{brand.Name}' đã tồn tại.");
                }

                brand.CreatedAt = DateTime.UtcNow;
                brand.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Brands.AddAsync(brand);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã tạo brand mới: {BrandName} (ID: {BrandId})", brand.Name, brand.BrandId);
                return brand;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo brand: {BrandName}", brand.Name);
                throw;
            }
        }

        public async Task<Brand> UpdateBrandAsync(Brand brand)
        {
            try
            {
                var existingBrand = await _unitOfWork.Brands.GetByIdAsync(brand.BrandId);
                if (existingBrand == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy brand với ID: {brand.BrandId}");
                }

                // Kiểm tra trùng tên (trừ chính nó)
                if (await BrandNameExistsAsync(brand.Name, brand.BrandId))
                {
                    throw new InvalidOperationException($"Brand với tên '{brand.Name}' đã tồn tại.");
                }

                existingBrand.Name = brand.Name;
                existingBrand.Description = brand.Description;
                existingBrand.LogoUrl = brand.LogoUrl;
                existingBrand.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Brands.Update(existingBrand);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã cập nhật brand: {BrandName} (ID: {BrandId})", brand.Name, brand.BrandId);
                return existingBrand;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật brand ID: {BrandId}", brand.BrandId);
                throw;
            }
        }

        public async Task<bool> DeleteBrandAsync(int id)
        {
            try
            {
                var brand = await _unitOfWork.Brands.GetByIdAsync(id);
                if (brand == null)
                {
                    return false;
                }

                // Kiểm tra có cars đang sử dụng brand này không
                var carCount = await GetCarCountByBrandAsync(id);
                if (carCount > 0)
                {
                    throw new InvalidOperationException($"Không thể xóa brand này vì có {carCount} xe đang sử dụng.");
                }

                _unitOfWork.Brands.Remove(brand);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã xóa brand: {BrandName} (ID: {BrandId})", brand.Name, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa brand ID: {BrandId}", id);
                throw;
            }
        }

        public async Task<bool> BrandExistsAsync(int id)
        {
            return await _unitOfWork.Brands.ExistsAsync(id);
        }

        public async Task<bool> BrandNameExistsAsync(string name, int? excludeId = null)
        {
            var existingBrand = await _unitOfWork.Brands.GetByNameAsync(name);
            if (existingBrand == null)
            {
                return false;
            }

            return excludeId == null || existingBrand.BrandId != excludeId;
        }

        public async Task<int> GetCarCountByBrandAsync(int brandId)
        {
            return await _unitOfWork.Brands.GetCarCountByBrandAsync(brandId);
        }

        public async Task<IEnumerable<Brand>> GetBrandsWithCarsAsync()
        {
            return await _unitOfWork.Brands.GetBrandsWithCarsAsync();
        }
    }
}