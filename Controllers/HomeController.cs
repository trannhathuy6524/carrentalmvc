using System.Diagnostics;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace carrentalmvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICarService _carService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IReviewService _reviewService;

        public HomeController(
            ILogger<HomeController> logger,
            ICarService carService,
            IBrandService brandService,
            ICategoryService categoryService,
            IReviewService reviewService)
        {
            _logger = logger;
            _carService = carService;
            _brandService = brandService;
            _categoryService = categoryService;
            _reviewService = reviewService;
        }

        // Trang chủ công khai - không cần đăng nhập
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
            string? province,
            DateTime? startDate,
            DateTime? endDate,
            bool? withDriver,
            int page = 1,
            int pageSize = 12)
        {
            try
            {
                // ✅ Hiển thị cả xe Available VÀ Rented (cho phép đặt trước)
                var allCars = await _carService.GetAllCarsAsync();
                
                // Filter by status: Available or Rented (allow booking for future)
                var carsQuery = allCars
                    .Where(c => c.IsActive && 
                                (c.Status == CarStatus.Available || 
                                 c.Status == CarStatus.Rented))
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    carsQuery = carsQuery.Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (c.Description != null && c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
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

                // ✅ Lọc theo tỉnh/thành phố
                if (!string.IsNullOrEmpty(province))
                {
                    carsQuery = carsQuery.Where(c => 
                        !string.IsNullOrEmpty(c.Location) && 
                        c.Location.Contains(province, StringComparison.OrdinalIgnoreCase));
                }

                // ✅ Lọc theo ngày thuê (kiểm tra xe có sẵn trong khoảng thời gian)
                if (startDate.HasValue && endDate.HasValue)
                {
                    // Get all cars first
                    var allCarsForDateFilter = carsQuery.ToList();
                    var carsAvailableInPeriod = new List<Car>();
                    
                    foreach (var car in allCarsForDateFilter)
                    {
                        // Lấy các rental đang active hoặc confirmed trong khoảng thời gian
                        var conflictingRentals = car.Rentals?.Where(r =>
                            (r.Status == RentalStatus.Active || 
                             r.Status == RentalStatus.Confirmed ||
                             r.Status == RentalStatus.Pending) &&
                            // Kiểm tra overlap: (StartA <= EndB) && (EndA >= StartB)
                            r.StartDate <= endDate.Value &&
                            r.EndDate >= startDate.Value
                        ).ToList() ?? new List<Rental>();

                        // Nếu không có conflict thì xe available
                        if (!conflictingRentals.Any())
                        {
                            carsAvailableInPeriod.Add(car);
                        }
                    }
                    
                    var finalCount = carsAvailableInPeriod.Count;
                    
                    // Pagination
                    var paginatedCars = carsAvailableInPeriod
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

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
                    ViewBag.Province = province;
                    ViewBag.StartDate = startDate;
                    ViewBag.EndDate = endDate;
                    ViewBag.WithDriver = withDriver;

                    // Pagination
                    ViewBag.PageNumber = page;
                    ViewBag.PageSize = pageSize;
                    ViewBag.TotalCount = finalCount;
                    ViewBag.TotalPages = (int)Math.Ceiling(finalCount / (double)pageSize);
                    ViewBag.HasPreviousPage = page > 1;
                    ViewBag.HasNextPage = page < ViewBag.TotalPages;

                    // Get reviews for rating display
                    var ratingDict = new Dictionary<int, (double avgRating, int count)>();
                    foreach (var car in paginatedCars)
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
                    ViewBag.IsGuestUser = !User.Identity?.IsAuthenticated ?? true;
                    ViewBag.ShowLoginPrompt = !User.Identity?.IsAuthenticated ?? true;
                    ViewBag.CanRent = User.Identity?.IsAuthenticated ?? false;

                    return View(paginatedCars);
                }
                else
                {
                    // No date filter - show all
                    var totalCount = carsQuery.Count();

                    var paginatedCars = carsQuery
                        .OrderByDescending(c => c.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

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
                    ViewBag.Province = province;
                    ViewBag.StartDate = startDate;
                    ViewBag.EndDate = endDate;
                    ViewBag.WithDriver = withDriver;

                    // Pagination
                    ViewBag.PageNumber = page;
                    ViewBag.PageSize = pageSize;
                    ViewBag.TotalCount = totalCount;
                    ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                    ViewBag.HasPreviousPage = page > 1;
                    ViewBag.HasNextPage = page < ViewBag.TotalPages;

                    // Get reviews for rating display
                    var ratingDict = new Dictionary<int, (double avgRating, int count)>();
                    foreach (var car in paginatedCars)
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
                    ViewBag.IsGuestUser = !User.Identity?.IsAuthenticated ?? true;
                    ViewBag.ShowLoginPrompt = !User.Identity?.IsAuthenticated ?? true;
                    ViewBag.CanRent = User.Identity?.IsAuthenticated ?? false;

                    return View(paginatedCars);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page with cars");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách xe.";
                
                ViewBag.PageNumber = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = 0;
                ViewBag.TotalPages = 0;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                ViewBag.IsGuestUser = !User.Identity?.IsAuthenticated ?? true;
                ViewBag.ShowLoginPrompt = !User.Identity?.IsAuthenticated ?? true;
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                
                return View(new List<Car>());
            }
        }

        // Chi tiết xe - công khai, không cần đăng nhập
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var car = await _carService.GetCarWithDetailsAsync(id);
                
                // ✅ Allow viewing Available or Rented cars (for future bookings)
                if (car == null || !car.IsActive)
                {
                    TempData["ErrorMessage"] = "Xe không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                // Only block if car is in Maintenance or PendingApproval
                if (car.Status == CarStatus.Maintenance || car.Status == CarStatus.PendingApproval)
                {
                    TempData["ErrorMessage"] = "Xe này hiện không khả dụng.";
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

                // Helper functions
                ViewBag.GetTransmissionText = new Func<Transmission?, string?>(GetTransmissionText);
                ViewBag.GetFuelTypeText = new Func<FuelType?, string?>(GetFuelTypeText);
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);

                // User info
                ViewBag.IsGuestUser = !User.Identity?.IsAuthenticated ?? true;
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

        // Action chuyển hướng đến đăng ký
        public IActionResult Register()
        {
            TempData["InfoMessage"] = "Đăng ký tài khoản để có thể thuê xe và sử dụng đầy đủ tính năng.";
            return Redirect("/Identity/Account/Register");
        }

        // Action chuyển hướng đến đăng nhập
        public IActionResult Login()
        {
            TempData["InfoMessage"] = "Đăng nhập để thuê xe và quản lý đơn hàng của bạn.";
            return Redirect("/Identity/Account/Login");
        }

        // Trang giới thiệu dịch vụ
        public IActionResult About()
        {
            return View();
        }

        // Trang liên hệ
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Test3D()
        {
            return View();
        }

        #region Private Helper Methods

        /// <summary>
        /// Get related cars based on same category or brand
        /// </summary>
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