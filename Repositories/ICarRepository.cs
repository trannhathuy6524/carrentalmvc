using carrentalmvc.Models;
using carrentalmvc.Models.Enums;

namespace carrentalmvc.Repositories
{
    public interface ICarRepository : IRepository<Car>
    {
        Task<IEnumerable<Car>> GetAvailableCarsAsync();
        Task<IEnumerable<Car>> GetCarsByOwnerAsync(string ownerId);
        Task<IEnumerable<Car>> GetCarsByBrandAsync(int brandId);
        Task<IEnumerable<Car>> GetCarsByCategoryAsync(int categoryId);
        Task<IEnumerable<Car>> GetCarsByStatusAsync(CarStatus status);
        Task<Car?> GetCarWithDetailsAsync(int carId);
        Task<IEnumerable<Car>> SearchCarsAsync(string searchTerm);
        Task<IEnumerable<Car>> GetFeaturedCarsAsync(int count = 10);
        Task<(IEnumerable<Car> cars, int totalCount)> SearchCarsAsync(
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
            CarStatus? status = null);
        Task AddCar3DModelAsync(CarModel3D carModel3D);
        Task<CarModel3D?> GetCar3DModelAsync(int carId);
        void UpdateCar3DModel(CarModel3D carModel3D);
        void RemoveCar3DModel(CarModel3D carModel3D);

    }
}