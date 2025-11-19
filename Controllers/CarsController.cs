using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace carrentalmvc.Controllers
{
    public class CarsController : Controller
    {
        private readonly ICarService _carService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IFeatureService _featureService;
        private readonly IReviewService _reviewService;
        private readonly ILogger<CarsController> _logger;

        public CarsController(
            ICarService carService,
            IBrandService brandService,
            ICategoryService categoryService,
            IFeatureService featureService,
            IReviewService reviewService,
            ILogger<CarsController> logger)
        {
            _carService = carService;
            _brandService = brandService;
            _categoryService = categoryService;
            _featureService = featureService;
            _reviewService = reviewService;
            _logger = logger;
        }

        // GET: Cars (Public - similar to Home/Index but with route /Cars)
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? brandId,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            int? minYear,
            int? maxYear,
            FuelType? fuelType,
            Transmission? transmission,
            int page = 1,
            int pageSize = 12)
        {
            try
            {
                // Only show available and active cars for public view
                var (cars, totalCount) = await _carService.SearchCarsAsync(
                    searchTerm: searchTerm,
                    brandId: brandId,
                    categoryId: categoryId,
                    minPrice: minPrice,
                    maxPrice: maxPrice,
                    minYear: minYear,
                    maxYear: maxYear,
                    fuelType: fuelType,
                    transmission: transmission,
                    pageNumber: page,
                    pageSize: pageSize,
                    status: CarStatus.Available
                );

                // Filter active cars only
                var availableCars = cars.Where(c => c.IsActive).ToList();
                var finalCount = availableCars.Count;

                // Load dropdown data for filters
                var brands = await _brandService.GetActiveBrandsAsync();
                var categories = await _categoryService.GetActiveCategoriesAsync();

                ViewBag.Brands = new SelectList(brands, "BrandId", "Name", brandId);
                ViewBag.Categories = new SelectList(categories, "CategoryId", "Name", categoryId);

                // Pass filter values to ViewBag
                ViewBag.SearchTerm = searchTerm;
                ViewBag.BrandId = brandId;
                ViewBag.CategoryId = categoryId;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.MinYear = minYear;
                ViewBag.MaxYear = maxYear;
                ViewBag.FuelType = fuelType;
                ViewBag.Transmission = transmission;

                // Pagination
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = finalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(finalCount / (double)pageSize);
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < ViewBag.TotalPages;

                // Get ratings for all cars
                var ratingDict = new Dictionary<int, (double avgRating, int count)>();
                foreach (var car in availableCars)
                {
                    var reviews = await _reviewService.GetReviewsByCarAsync(car.CarId);
                    var activeReviews = reviews.Where(r => r.IsActive).ToList();
                    var avgRating = activeReviews.Any() ? activeReviews.Average(r => r.Rating) : 0;
                    var count = activeReviews.Count;
                    ratingDict[car.CarId] = (avgRating, count);
                }

                ViewBag.CarRatings = ratingDict;

                // Helper functions
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);

                // User info
                ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
                ViewBag.CanRent = User.Identity?.IsAuthenticated ?? false;
                ViewBag.ShowLoginPrompt = !User.Identity?.IsAuthenticated ?? true;

                return View(availableCars);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cars list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách xe.";

                ViewBag.PageNumber = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = 0;
                ViewBag.TotalPages = 0;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);

                return View(new List<Car>());
            }
        }

        // GET: Cars/Details/5 (Public)
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var car = await _carService.GetCarWithDetailsAsync(id);
                if (car == null || !car.IsActive || car.Status != CarStatus.Available)
                {
                    TempData["ErrorMessage"] = "Xe không tồn tại hoặc không còn sẵn sàng cho thuê.";
                    return RedirectToAction(nameof(Index));
                }

                // Get reviews for this car
                var reviews = await _reviewService.GetReviewsByCarAsync(id);
                var activeReviews = reviews.Where(r => r.IsActive).OrderByDescending(r => r.CreatedAt).ToList();

                var averageRating = activeReviews.Any() ? activeReviews.Average(r => r.Rating) : 0.0;
                var reviewCount = activeReviews.Count;

                // Rating distribution
                var ratingDistribution = new Dictionary<int, int>();
                for (int i = 1; i <= 5; i++)
                {
                    ratingDistribution[i] = activeReviews.Count(r => r.Rating == i);
                }

                // Get related cars (same category or brand, limit 4)
                var relatedCars = await GetRelatedCarsAsync(car, 4);

                // Pass data to ViewBag
                ViewBag.Reviews = activeReviews;
                ViewBag.AverageRating = averageRating;
                ViewBag.ReviewCount = reviewCount;
                ViewBag.RatingDistribution = ratingDistribution;
                ViewBag.RelatedCars = relatedCars;

                // Get car images
                var imageUrls = car.CarImages?.OrderByDescending(img => img.IsPrimary)
                    .ThenBy(img => img.DisplayOrder)
                    .Select(img => img.ImageUrl)
                    .ToList() ?? new List<string>();

                ViewBag.ImageUrls = imageUrls;

                // Get features
                var features = car.CarFeatures?.Select(cf => cf.Feature).ToList() ?? new List<Feature>();
                ViewBag.Features = features;

                // Helper functions
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);

                // User info
                ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
                ViewBag.CanRent = User.Identity?.IsAuthenticated ?? false;
                ViewBag.ShowLoginPrompt = !User.Identity?.IsAuthenticated ?? true;

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car details for ID: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Get related cars based on same category or brand
        /// </summary>
        private async Task<List<Car>> GetRelatedCarsAsync(Car currentCar, int count)
        {
            try
            {
                // Get cars from same category or brand
                var (cars, _) = await _carService.SearchCarsAsync(
                    null,
                    currentCar.BrandId,
                    currentCar.CategoryId,
                    null, null, null, null, null, null,
                    1,
                    count + 1, // Get one extra in case current car is included
                    CarStatus.Available
                );

                return cars
                    .Where(c => c.CarId != currentCar.CarId && c.IsActive) // Exclude current car
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related cars");
                return new List<Car>();
            }
        }

        private string GetCarStatusText(CarStatus status)
        {
            return status switch
            {
                CarStatus.Available => "Sẵn sàng",
                CarStatus.Rented => "Đang thuê",
                CarStatus.Maintenance => "Bảo trì",
                CarStatus.PendingApproval => "Chờ duyệt",
                CarStatus.Reserved => "Đã đặt",
                _ => "Không xác định"
            };
        }

        private string? GetTransmissionText(Transmission? transmission)
        {
            return transmission switch
            {
                Transmission.Manual => "Số sàn",
                Transmission.Automatic => "Số tự động",
                Transmission.CVT => "CVT",
                Transmission.SemiAutomatic => "Bán tự động",
                _ => null
            };
        }

        private string? GetFuelTypeText(FuelType? fuelType)
        {
            return fuelType switch
            {
                FuelType.Gasoline => "Xăng",
                FuelType.Diesel => "Dầu",
                FuelType.Electric => "Điện",
                FuelType.Hybrid => "Hybrid",
                FuelType.LPG => "LPG",
                _ => null
            };
        }

        #endregion
    }
}