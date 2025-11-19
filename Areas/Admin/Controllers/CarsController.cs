using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class CarsController : Controller
    {
        private readonly ICarService _carService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IFeatureService _featureService;
        private readonly ILogger<CarsController> _logger;

        public CarsController(
            ICarService carService,
            IBrandService brandService,
            ICategoryService categoryService,
            IFeatureService featureService,
            ILogger<CarsController> logger)
        {
            _carService = carService;
            _brandService = brandService;
            _categoryService = categoryService;
            _featureService = featureService;
            _logger = logger;
        }

        // GET: Admin/Cars
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
            CarStatus? status,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var (cars, totalCount) = await _carService.SearchCarsAsync(
                    searchTerm,
                    brandId,
                    categoryId,
                    minPrice,
                    maxPrice,
                    minYear,
                    maxYear,
                    fuelType,
                    transmission,
                    page,
                    pageSize,
                    status
                );

                // Load dropdown data cho filter
                await LoadFilterDropdownsAsync(brandId, categoryId);

                // Sử dụng ViewBag để truyền dữ liệu filter và phân trang
                ViewBag.SearchTerm = searchTerm;
                ViewBag.BrandId = brandId;
                ViewBag.CategoryId = categoryId;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.MinYear = minYear;
                ViewBag.MaxYear = maxYear;
                ViewBag.FuelType = fuelType;
                ViewBag.Transmission = transmission;
                ViewBag.Status = status;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Thống kê tổng quan
                var allCars = await _carService.GetAllCarsAsync();
                ViewBag.TotalCars = allCars.Count();
                ViewBag.TotalAvailableCars = allCars.Count(c => c.Status == CarStatus.Available);
                ViewBag.TotalRentedCars = allCars.Count(c => c.Status == CarStatus.Rented);
                ViewBag.TotalPendingCars = allCars.Count(c => c.Status == CarStatus.PendingApproval);
                ViewBag.TotalMaintenanceCars = allCars.Count(c => c.Status == CarStatus.Maintenance);

                return View(cars);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cars list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách xe.";
                return View(new List<Car>());
            }
        }

        // GET: Admin/Cars/PendingApproval
        public async Task<IActionResult> PendingApproval(int page = 1, int pageSize = 10)
        {
            try
            {
                var allCars = await _carService.GetAllCarsAsync();
                var pendingCars = allCars.Where(c => c.Status == CarStatus.PendingApproval);

                var totalCount = pendingCars.Count();
                var pagedCars = pendingCars
                    .OrderBy(c => c.CreatedAt) // Cũ nhất trước (First In, First Out)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Sử dụng ViewBag cho phân trang
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                ViewData["Title"] = "Xe chờ duyệt";
                return View("PendingApproval", pagedCars);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending approval cars");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách xe chờ duyệt.";
                return View("PendingApproval", new List<Car>());
            }
        }

        // GET: Admin/Cars/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var car = await _carService.GetCarWithDetailsAsync(id);
                if (car == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền thông tin bổ sung qua ViewBag
                ViewBag.StatusText = GetCarStatusText(car.Status);
                ViewBag.TransmissionText = GetTransmissionText(car.Transmission);
                ViewBag.FuelTypeText = GetFuelTypeText(car.FuelType);
                ViewBag.PrimaryImageUrl = car.CarImages?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl;
                ViewBag.ImageUrls = car.CarImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>();

                // Thông tin features
                ViewBag.Features = car.CarFeatures?.Select(cf => cf.Feature).ToList() ?? new List<Feature>();

                // Thông tin mô hình 3D
                ViewBag.Model3DUrl = car.CarModel3D?.ModelUrl;
                ViewBag.Model3DFileFormat = car.CarModel3D?.FileFormat;

                // Thông tin thống kê
                ViewBag.TotalRentals = car.Rentals?.Count ?? 0;
                ViewBag.TotalReviews = car.Reviews?.Count ?? 0;
                ViewBag.AverageRating = car.Reviews?.Any() == true ? car.Reviews.Average(r => r.Rating) : 0;

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car details for ID: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Cars/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? notes)
        {
            try
            {
                var car = await _carService.GetCarByIdAsync(id);
                if (car == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe.";
                    return RedirectToAction(nameof(PendingApproval));
                }

                if (car.Status != CarStatus.PendingApproval)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể duyệt xe ở trạng thái 'Chờ duyệt'.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _carService.ApproveCarAsync(id, notes);
                TempData["SuccessMessage"] = $"Đã duyệt xe '{car.Name}' thành công.";

                return RedirectToAction(nameof(PendingApproval));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving car: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi duyệt xe.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Cars/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var car = await _carService.GetCarByIdAsync(id);
                if (car == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe.";
                    return RedirectToAction(nameof(PendingApproval));
                }

                if (car.Status != CarStatus.PendingApproval)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể từ chối xe ở trạng thái 'Chờ duyệt'.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _carService.RejectCarAsync(id, reason);
                TempData["SuccessMessage"] = $"Đã từ chối xe '{car.Name}' với lý do: {reason}";

                return RedirectToAction(nameof(PendingApproval));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting car: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi từ chối xe.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Cars/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, CarStatus status)
        {
            try
            {
                await _carService.UpdateCarStatusAsync(id, status);
                TempData["SuccessMessage"] = "Trạng thái xe đã được cập nhật thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating car status: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái xe.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Cars/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var car = await _carService.GetCarWithDetailsAsync(id);
                if (car == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền thông tin bổ sung qua ViewBag
                ViewBag.StatusText = GetCarStatusText(car.Status);
                ViewBag.TransmissionText = GetTransmissionText(car.Transmission);
                ViewBag.FuelTypeText = GetFuelTypeText(car.FuelType);
                ViewBag.PrimaryImageUrl = car.CarImages?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl;
                ViewBag.TotalRentals = car.Rentals?.Count ?? 0;
                ViewBag.TotalReviews = car.Reviews?.Count ?? 0;
                ViewBag.HasActiveRentals = car.Rentals?.Any(r => r.Status == RentalStatus.Active || r.Status == RentalStatus.Confirmed) ?? false;

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car for delete: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _carService.DeleteCarAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Xe đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe để xóa.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting car: {CarId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa xe.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task LoadFilterDropdownsAsync(int? selectedBrandId, int? selectedCategoryId)
        {
            var brands = await _brandService.GetAllBrandsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();

            ViewBag.Brands = new SelectList(brands, "BrandId", "Name", selectedBrandId);
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name", selectedCategoryId);
        }

        private string GetCarStatusText(CarStatus status)
        {
            return status switch
            {
                CarStatus.Available => "Sẵn sàng",
                CarStatus.Rented => "Đang thuê",
                CarStatus.Maintenance => "Bảo trì",
                CarStatus.PendingApproval => "Chờ duyệt",
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
    }
}