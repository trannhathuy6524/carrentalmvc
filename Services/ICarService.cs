using carrentalmvc.Models;
using carrentalmvc.Models.Enums;

namespace carrentalmvc.Services
{
    public interface ICarService
    {
        Task<IEnumerable<Car>> GetAllCarsAsync();
        Task<IEnumerable<Car>> GetAvailableCarsAsync();
        Task<Car?> GetCarByIdAsync(int id);
        Task<Car?> GetCarWithDetailsAsync(int id);
        Task<IEnumerable<Car>> GetCarsByOwnerAsync(string ownerId);
        Task<IEnumerable<Car>> GetCarsByBrandAsync(int brandId);
        Task<IEnumerable<Car>> GetCarsByCategoryAsync(int categoryId);
        Task<IEnumerable<Car>> GetFeaturedCarsAsync(int count = 10);
        Task<Car> CreateCarAsync(Car car);
        Task<Car> UpdateCarAsync(Car car);
        Task<bool> DeleteCarAsync(int id);
        Task<bool> CarExistsAsync(int id);
        Task<bool> IsCarAvailableForRentAsync(int carId, DateTime startDate, DateTime endDate);
        Task<CarStatus> UpdateCarStatusAsync(int carId, CarStatus status);
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
        Task AddCarFeaturesAsync(int carId, IEnumerable<int> featureIds);
        Task RemoveCarFeaturesAsync(int carId, IEnumerable<int> featureIds);
        Task<IEnumerable<Feature>> GetCarFeaturesAsync(int carId);
        Task<bool> ApproveCarAsync(int carId, string? notes = null);
        Task<bool> RejectCarAsync(int carId, string reason);
        Task<CarModel3D> AddCar3DModelAsync(CarModel3D carModel3D);
        Task<CarModel3D?> GetCar3DModelAsync(int carId);
        Task<bool> UpdateCar3DModelAsync(CarModel3D carModel3D);
        Task<bool> RemoveCar3DModelAsync(int carId);
        Task<CarImage> AddCarImageAsync(CarImage carImage);
        Task<CarImage?> GetCarImageByIdAsync(int imageId);
        Task<bool> DeleteCarImageAsync(int imageId);
        Task<bool> UpdateCarImageAsync(CarImage carImage);
        Task<IEnumerable<CarImage>> GetCarImagesAsync(int carId);
        Task<bool> SetPrimaryImageAsync(int carId, int imageId);

    }
}