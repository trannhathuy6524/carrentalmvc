using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace carrentalmvc.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = RoleConstants.Customer)]
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

        // GET: Customer/Cars
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
                // ✅ Show Available AND Rented cars (for future bookings)
                // Customers can see rented cars and book for later dates
                var allCars = await _carService.GetAllCarsAsync();
                
                // Filter by status: Available or Rented (allow booking for future)
                var carsQuery = allCars
                    .Where(c => c.IsActive && (c.Status == CarStatus.Available || c.Status == CarStatus.Rented))
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    carsQuery = carsQuery.Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.LicensePlate.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                if (brandId.HasValue)
                {
                    carsQuery = carsQuery.Where(c => c.BrandId == brandId.Value);
                }

                if (categoryId.HasValue)
                {
                    carsQuery = carsQuery.Where(c => c.CategoryId == categoryId.Value);
                }

                if (minPrice.HasValue)
                {
                    carsQuery = carsQuery.Where(c => c.PricePerDay >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    carsQuery = carsQuery.Where(c => c.PricePerDay <= maxPrice.Value);
                }

                if (minYear.HasValue)
                {
                    carsQuery = carsQuery.Where(c => c.Year >= minYear.Value);
                }

                if (maxYear.HasValue)
                {
                    carsQuery = carsQuery.Where(c => c.Year <= maxYear.Value);
                }

                if (fuelType.HasValue)
                {
                    carsQuery = carsQuery.Where(c => c.FuelType == fuelType.Value);
                }

                if (transmission.HasValue)
                {
                    carsQuery = carsQuery.Where(c => c.Transmission == transmission.Value);
                }

                // Get total count before pagination
                var totalCount = carsQuery.Count();

                // Apply pagination
                var cars = carsQuery
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

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
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < ViewBag.TotalPages;

                // Helper functions
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);

                return View(cars.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cars list for customer");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách xe.";

                ViewBag.PageNumber = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = 0;
                ViewBag.TotalPages = 0;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);

                return View(new List<Car>());
            }
        }

        // GET: Customer/Cars/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var car = await _carService.GetCarWithDetailsAsync(id);
                
                // ✅ Allow viewing Available or Rented cars (for future bookings)
                if (car == null || !car.IsActive)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe.";
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                // Only block if car is in Maintenance or PendingApproval
                if (car.Status == CarStatus.Maintenance || car.Status == CarStatus.PendingApproval)
                {
                    TempData["ErrorMessage"] = "Xe này hiện không khả dụng.";
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                // Get reviews for this car
                var reviews = await _reviewService.GetReviewsByCarAsync(id);
                var activeReviews = reviews.Where(r => r.IsActive).OrderByDescending(r => r.CreatedAt).ToList();

                // Calculate rating stats
                var averageRating = activeReviews.Any() ? activeReviews.Average(r => r.Rating) : 0.0;
                var reviewCount = activeReviews.Count;

                // Rating distribution
                var ratingDistribution = new Dictionary<int, int>();
                for (int i = 1; i <= 5; i++)
                {
                    ratingDistribution[i] = activeReviews.Count(r => r.Rating == i);
                }

                // Pass data to ViewBag
                ViewBag.Reviews = activeReviews;
                ViewBag.AverageRating = averageRating;
                ViewBag.ReviewCount = reviewCount;
                ViewBag.RatingDistribution = ratingDistribution;

                // Get related cars (same category or brand)
                var relatedCars = await GetRelatedCarsAsync(car, 4);
                ViewBag.RelatedCars = relatedCars;

                // Helper functions
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car details for ID: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xe.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        #region Private Helper Methods

        private async Task<List<Car>> GetRelatedCarsAsync(Car currentCar, int count)
        {
            try
            {
                // ✅ Get Available and Rented cars (allow future bookings)
                var allCars = await _carService.GetAllCarsAsync();
                
                var relatedCars = allCars
                    .Where(c => 
                        c.CarId != currentCar.CarId && 
                        c.IsActive &&
                        (c.Status == CarStatus.Available || c.Status == CarStatus.Rented) &&
                        (c.BrandId == currentCar.BrandId || c.CategoryId == currentCar.CategoryId))
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(count)
                    .ToList();

                return relatedCars;
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
                _ => null
            };
        }

        private string? GetFuelTypeText(FuelType? fuelType)
        {
            return fuelType switch
            {
                FuelType.Gasoline => "Xăng",
                FuelType.Diesel => "Dầu",
                FuelType.Hybrid => "Hybrid",
                FuelType.Electric => "Điện",
                _ => null
            };
        }

        #endregion
    }
}