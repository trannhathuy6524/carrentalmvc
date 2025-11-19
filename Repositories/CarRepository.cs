using carrentalmvc.Data;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class CarRepository : Repository<Car>, ICarRepository
    {
        public CarRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Car>> GetAvailableCarsAsync()
        {
            return await _dbSet
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.CarImages.Where(img => img.IsPrimary))
                .Where(c => c.IsActive && c.Status == CarStatus.Available)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> GetCarsByOwnerAsync(string ownerId)
        {
            return await _dbSet
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.CarImages.Where(img => img.IsPrimary))
                .Where(c => c.OwnerId == ownerId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> GetCarsByBrandAsync(int brandId)
        {
            return await _dbSet
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.CarImages.Where(img => img.IsPrimary))
                .Where(c => c.BrandId == brandId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> GetCarsByCategoryAsync(int categoryId)
        {
            return await _dbSet
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.CarImages.Where(img => img.IsPrimary))
                .Where(c => c.CategoryId == categoryId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> GetCarsByStatusAsync(CarStatus status)
        {
            return await _dbSet
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.Owner)
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<Car?> GetCarWithDetailsAsync(int carId)
        {
            return await _dbSet
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.Owner)
                .Include(c => c.CarImages.OrderBy(img => img.DisplayOrder))
                .Include(c => c.CarFeatures)
                    .ThenInclude(cf => cf.Feature)
                .Include(c => c.Reviews.Where(r => r.IsActive))
                    .ThenInclude(r => r.User)
                .Include(c => c.CarModel3D)
                .FirstOrDefaultAsync(c => c.CarId == carId);
        }

        public async Task<IEnumerable<Car>> SearchCarsAsync(string searchTerm)
        {
            return await _dbSet
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.CarImages.Where(img => img.IsPrimary))
                .Where(c => c.IsActive && c.Status == CarStatus.Available &&
                           (c.Name.Contains(searchTerm) ||
                            c.Description!.Contains(searchTerm) ||
                            c.Brand.Name.Contains(searchTerm) ||
                            c.Category.Name.Contains(searchTerm)))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> GetFeaturedCarsAsync(int count = 10)
        {
            return await _dbSet
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .Include(c => c.CarImages.Where(img => img.IsPrimary))
                .Where(c => c.IsActive && c.Status == CarStatus.Available)
                .OrderByDescending(c => c.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Car> cars, int totalCount)> SearchCarsAsync(
            string? searchTerm = null,
            int? brandId = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minYear = null,
            int? maxYear = null,
            FuelType? fuelType = null,
            Transmission? transmission = null,
            int pageNumber = 1,
            int pageSize = 10,
            CarStatus? status = null)
            {
                // Bắt đầu với query cơ bản
                var query = _dbSet.AsQueryable();

                // Áp dụng các bộ lọc TRƯỚC khi Include
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => c.Name.Contains(searchTerm) ||
                                            c.Description!.Contains(searchTerm) ||
                                            c.Brand.Name.Contains(searchTerm));
                }

                if (brandId.HasValue)
                    query = query.Where(c => c.BrandId == brandId.Value);

                if (categoryId.HasValue)
                    query = query.Where(c => c.CategoryId == categoryId.Value);

                if (minPrice.HasValue)
                    query = query.Where(c => c.PricePerDay >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(c => c.PricePerDay <= maxPrice.Value);

                if (minYear.HasValue)
                    query = query.Where(c => c.Year >= minYear.Value);

                if (maxYear.HasValue)
                    query = query.Where(c => c.Year <= maxYear.Value);

                if (fuelType.HasValue)
                    query = query.Where(c => c.FuelType == fuelType.Value);

                if (transmission.HasValue)
                    query = query.Where(c => c.Transmission == transmission.Value);

                if (status.HasValue)
                    query = query.Where(c => c.Status == status.Value);

                // Đếm TRƯỚC khi Include (tối ưu performance)
                var totalCount = await query.CountAsync();

                // Include SAU khi đã filter và count
                var cars = await query
                    .Include(c => c.Brand)
                    .Include(c => c.Category)
                    .Include(c => c.Owner)
                    .Include(c => c.CarImages.Where(img => img.IsPrimary))
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (cars, totalCount);
        }
        public void UpdateCar3DModel(CarModel3D carModel3D)
        {
            _context.CarModel3Ds.Update(carModel3D);
        }
        public async Task AddCar3DModelAsync(CarModel3D carModel3D)
        {
            await _context.CarModel3Ds.AddAsync(carModel3D);
        }
        public async Task<CarModel3D?> GetCar3DModelAsync(int carId)
        {
            return await _context.CarModel3Ds.FirstOrDefaultAsync(m => m.CarId == carId);
        }
        public void RemoveCar3DModel(CarModel3D carModel3D)
        {
            _context.CarModel3Ds.Remove(carModel3D);
        }
    }
}