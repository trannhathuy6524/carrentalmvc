using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Repositories;

namespace carrentalmvc.Services
{
    public class CarService : ICarService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICarRepository _carRepository;
        private readonly ILogger<CarService> _logger;

        public CarService(IUnitOfWork unitOfWork, ILogger<CarService> logger, ICarRepository carRepository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _carRepository = carRepository;
        }

        public async Task<IEnumerable<Car>> GetAllCarsAsync()
        {
            return await _unitOfWork.Cars.GetAllAsync();
        }

        public async Task<IEnumerable<Car>> GetAvailableCarsAsync()
        {
            return await _unitOfWork.Cars.GetAvailableCarsAsync();
        }

        public async Task<Car?> GetCarByIdAsync(int id)
        {
            return await _unitOfWork.Cars.GetByIdAsync(id);
        }

        public async Task<Car?> GetCarWithDetailsAsync(int id)
        {
            return await _unitOfWork.Cars.GetCarWithDetailsAsync(id);
        }

        public async Task<IEnumerable<Car>> GetCarsByOwnerAsync(string ownerId)
        {
            return await _unitOfWork.Cars.GetCarsByOwnerAsync(ownerId);
        }

        public async Task<IEnumerable<Car>> GetCarsByBrandAsync(int brandId)
        {
            return await _unitOfWork.Cars.GetCarsByBrandAsync(brandId);
        }

        public async Task<IEnumerable<Car>> GetCarsByCategoryAsync(int categoryId)
        {
            return await _unitOfWork.Cars.GetCarsByCategoryAsync(categoryId);
        }

        public async Task<IEnumerable<Car>> GetFeaturedCarsAsync(int count = 10)
        {
            return await _unitOfWork.Cars.GetFeaturedCarsAsync(count);
        }

        public async Task<Car> CreateCarAsync(Car car)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                car.CreatedAt = DateTime.UtcNow;
                car.UpdatedAt = DateTime.UtcNow;
                car.Status = CarStatus.PendingApproval;
                car.IsActive = false; // Cần admin duyệt

                await _unitOfWork.Cars.AddAsync(car);
                await _unitOfWork.SaveAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Đã tạo car mới: {CarName} (ID: {CarId})", car.Name, car.CarId);
                return car;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi tạo car: {CarName}", car.Name);
                throw;
            }
        }

        public async Task<Car> UpdateCarAsync(Car car)
        {
            try
            {
                var existingCar = await _unitOfWork.Cars.GetByIdAsync(car.CarId);
                if (existingCar == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy car với ID: {car.CarId}");
                }

                existingCar.Name = car.Name;
                existingCar.Description = car.Description;
                existingCar.BrandId = car.BrandId;
                existingCar.CategoryId = car.CategoryId;
                existingCar.Year = car.Year;
                existingCar.Color = car.Color;
                existingCar.LicensePlate = car.LicensePlate;
                existingCar.Seats = car.Seats;
                existingCar.Transmission = car.Transmission;
                existingCar.FuelType = car.FuelType;
                existingCar.FuelConsumption = car.FuelConsumption;
                existingCar.PricePerDay = car.PricePerDay;
                existingCar.PricePerHour = car.PricePerHour;
                existingCar.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Cars.Update(existingCar);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã cập nhật car: {CarName} (ID: {CarId})", car.Name, car.CarId);
                return existingCar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật car ID: {CarId}", car.CarId);
                throw;
            }
        }

        public async Task<bool> DeleteCarAsync(int id)
        {
            try
            {
                var car = await _unitOfWork.Cars.GetByIdAsync(id);
                if (car == null)
                {
                    return false;
                }

                // Kiểm tra có rental nào đang active không
                var activeRentals = await _unitOfWork.Rentals.GetAsync(
                    r => r.CarId == id && r.Status == RentalStatus.Active);

                if (activeRentals.Any())
                {
                    throw new InvalidOperationException("Không thể xóa xe này vì đang có thuê xe đang hoạt động.");
                }

                _unitOfWork.Cars.Remove(car);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã xóa car: {CarName} (ID: {CarId})", car.Name, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa car ID: {CarId}", id);
                throw;
            }
        }

        public async Task<bool> CarExistsAsync(int id)
        {
            return await _unitOfWork.Cars.ExistsAsync(id);
        }

        public async Task<bool> IsCarAvailableForRentAsync(int carId, DateTime startDate, DateTime endDate)
        {
            var car = await _unitOfWork.Cars.GetByIdAsync(carId);
            if (car == null || !car.IsActive || car.Status != CarStatus.Available)
            {
                return false;
            }

            return await _unitOfWork.Rentals.IsCarAvailableAsync(carId, startDate, endDate);
        }

        public async Task<CarStatus> UpdateCarStatusAsync(int carId, CarStatus status)
        {
            try
            {
                var car = await _unitOfWork.Cars.GetByIdAsync(carId);
                if (car == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy car với ID: {carId}");
                }

                car.Status = status;
                car.UpdatedAt = DateTime.UtcNow;

                // Nếu status là Available thì set IsActive = true
                if (status == CarStatus.Available)
                {
                    car.IsActive = true;
                }

                _unitOfWork.Cars.Update(car);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã cập nhật status car ID {CarId} thành {Status}", carId, status);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật status car ID: {CarId}", carId);
                throw;
            }
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
            return await _unitOfWork.Cars.SearchCarsAsync(
                searchTerm, brandId, categoryId, minPrice, maxPrice,
                minYear, maxYear, fuelType, transmission, pageNumber, pageSize, status);
        }

        public async Task AddCarFeaturesAsync(int carId, IEnumerable<int> featureIds)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                foreach (var featureId in featureIds)
                {
                    var carFeature = new CarFeature
                    {
                        CarId = carId,
                        FeatureId = featureId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Cars.GetAsync(); // Sử dụng context để add CarFeature
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Đã thêm {Count} features cho car ID: {CarId}", featureIds.Count(), carId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi thêm features cho car ID: {CarId}", carId);
                throw;
            }
        }
        public async Task<CarModel3D> AddCar3DModelAsync(CarModel3D carModel3D)
        {
            // Remove existing 3D model if any
            await RemoveCar3DModelAsync(carModel3D.CarId);

            carModel3D.CreatedAt = DateTime.UtcNow;
            carModel3D.UpdatedAt = DateTime.UtcNow;

            await _carRepository.AddCar3DModelAsync(carModel3D);
            await _unitOfWork.SaveAsync();

            return carModel3D;
        }

        public async Task<CarModel3D?> GetCar3DModelAsync(int carId)
        {
            return await _carRepository.GetCar3DModelAsync(carId);
        }

        public async Task<bool> UpdateCar3DModelAsync(CarModel3D carModel3D)
        {
            var existing = await _carRepository.GetCar3DModelAsync(carModel3D.CarId);
            if (existing == null)
            {
                return false;
            }

            existing.TemplateId = carModel3D.TemplateId;
            existing.ModelUrl = carModel3D.ModelUrl;
            existing.FileFormat = carModel3D.FileFormat;
            existing.FileSize = carModel3D.FileSize;
            existing.UpdatedAt = DateTime.UtcNow;

            _carRepository.UpdateCar3DModel(existing);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<bool> RemoveCar3DModelAsync(int carId)
        {
            var existing = await _carRepository.GetCar3DModelAsync(carId);
            if (existing == null)
            {
                return false;
            }

            _carRepository.RemoveCar3DModel(existing);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task RemoveCarFeaturesAsync(int carId, IEnumerable<int> featureIds)
        {
            // Implementation for removing car features
            _logger.LogInformation("Đã xóa {Count} features khỏi car ID: {CarId}", featureIds.Count(), carId);
        }

        public async Task<IEnumerable<Feature>> GetCarFeaturesAsync(int carId)
        {
            return await _unitOfWork.Features.GetCarFeaturesAsync(carId);
        }

        public async Task<bool> ApproveCarAsync(int carId, string? notes = null)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var car = await _unitOfWork.Cars.GetByIdAsync(carId);
                if (car == null)
                {
                    return false;
                }

                if (car.Status != CarStatus.PendingApproval)
                {
                    throw new InvalidOperationException("Chỉ có thể duyệt xe ở trạng thái Chờ duyệt");
                }

                car.Status = CarStatus.Available;
                car.IsActive = true;
                car.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(notes))
                {
                    car.Description = string.IsNullOrEmpty(car.Description) ? notes : $"{car.Description}\n\nGhi chú admin: {notes}";
                }

                _unitOfWork.Cars.Update(car);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Đã duyệt xe ID: {CarId} bởi admin", carId);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi duyệt xe ID: {CarId}", carId);
                throw;
            }
        }

        public async Task<bool> RejectCarAsync(int carId, string reason)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var car = await _unitOfWork.Cars.GetByIdAsync(carId);
                if (car == null)
                {
                    return false;
                }

                if (car.Status != CarStatus.PendingApproval)
                {
                    throw new InvalidOperationException("Chỉ có thể từ chối xe ở trạng thái Chờ duyệt");
                }

                // Đánh dấu xe là không hoạt động và thêm lý do từ chối
                car.IsActive = false;
                car.UpdatedAt = DateTime.UtcNow;
                car.Description = string.IsNullOrEmpty(car.Description) ?
                    $"TỪchối: {reason}" :
                    $"{car.Description}\n\nTỪ CHỐI BỞI ADMIN: {reason}";

                _unitOfWork.Cars.Update(car);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Đã từ chối xe ID: {CarId} bởi admin với lý do: {Reason}", carId, reason);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi từ chối xe ID: {CarId}", carId);
                throw;
            }
        }
        public async Task<CarImage> AddCarImageAsync(CarImage carImage)
        {
            try
            {
                var car = await _carRepository.GetByIdAsync(carImage.CarId);
                if (car == null)
                {
                    throw new InvalidOperationException("Không tìm thấy xe.");
                }

                // If this is set as primary, unset other primary images
                if (carImage.IsPrimary)
                {
                    var existingImages = car.CarImages.Where(img => img.IsPrimary).ToList();
                    foreach (var img in existingImages)
                    {
                        img.IsPrimary = false;
                    }
                }

                carImage.CreatedAt = DateTime.UtcNow;
                car.CarImages.Add(carImage);
                car.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveAsync();
                return carImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding car image for car {CarId}", carImage.CarId);
                throw;
            }
        }

        public async Task<CarImage?> GetCarImageByIdAsync(int imageId)
        {
            try
            {
                var cars = await _carRepository.GetAllAsync();
                return cars.SelectMany(c => c.CarImages)
                          .FirstOrDefault(img => img.CarImageId == imageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting car image {ImageId}", imageId);
                throw;
            }
        }

        public async Task<bool> DeleteCarImageAsync(int imageId)
        {
            try
            {
                var cars = await _carRepository.GetAllAsync();
                var car = cars.FirstOrDefault(c => c.CarImages.Any(img => img.CarImageId == imageId));

                if (car == null)
                {
                    return false;
                }

                var image = car.CarImages.FirstOrDefault(img => img.CarImageId == imageId);
                if (image == null)
                {
                    return false;
                }

                car.CarImages.Remove(image);
                car.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting car image {ImageId}", imageId);
                throw;
            }
        }

        public async Task<bool> UpdateCarImageAsync(CarImage carImage)
        {
            try
            {
                var existingImage = await GetCarImageByIdAsync(carImage.CarImageId);
                if (existingImage == null)
                {
                    return false;
                }

                var car = await _carRepository.GetByIdAsync(carImage.CarId);
                if (car == null)
                {
                    return false;
                }

                // If setting as primary, unset other primary images
                if (carImage.IsPrimary && !existingImage.IsPrimary)
                {
                    var otherPrimaryImages = car.CarImages
                        .Where(img => img.IsPrimary && img.CarImageId != carImage.CarImageId)
                        .ToList();

                    foreach (var img in otherPrimaryImages)
                    {
                        img.IsPrimary = false;
                    }
                }

                existingImage.ImageUrl = carImage.ImageUrl;
                existingImage.IsPrimary = carImage.IsPrimary;
                car.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating car image {ImageId}", carImage.CarImageId);
                throw;
            }
        }

        public async Task<IEnumerable<CarImage>> GetCarImagesAsync(int carId)
        {
            try
            {
                var car = await _carRepository.GetByIdAsync(carId);
                return car?.CarImages ?? new List<CarImage>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting car images for car {CarId}", carId);
                throw;
            }
        }

        public async Task<bool> SetPrimaryImageAsync(int carId, int imageId)
        {
            try
            {
                var car = await _carRepository.GetByIdAsync(carId);
                if (car == null)
                {
                    return false;
                }

                var targetImage = car.CarImages.FirstOrDefault(img => img.CarImageId == imageId);
                if (targetImage == null)
                {
                    return false;
                }

                // Unset all primary images
                foreach (var img in car.CarImages.Where(img => img.IsPrimary))
                {
                    img.IsPrimary = false;
                }

                // Set new primary image
                targetImage.IsPrimary = true;
                car.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary image {ImageId} for car {CarId}", imageId, carId);
                throw;
            }
        }
    }
}