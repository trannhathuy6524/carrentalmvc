using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class DashboardController : Controller
    {
        private readonly ICarService _carService;
        private readonly IRentalService _rentalService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ICarService carService,
            IRentalService rentalService,
            IPaymentService paymentService,
            ILogger<DashboardController> logger)
        {
            _carService = carService;
            _rentalService = rentalService;
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpGet]
        [Route("Admin/Dashboard")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy dữ liệu cơ bản
                var allCars = await _carService.GetAllCarsAsync();
                var allRentals = await _rentalService.GetAllRentalsAsync();
                var activeRentals = await _rentalService.GetActiveRentalsAsync();

                // Thống kê cơ bản qua ViewBag
                ViewBag.TotalCars = allCars.Count();
                ViewBag.ActiveCars = allCars.Count(c => c.IsActive);
                ViewBag.TotalRentals = allRentals.Count();
                ViewBag.ActiveRentals = activeRentals.Count();
                ViewBag.PendingRentals = allRentals.Count(r => r.Status == RentalStatus.Pending);
                ViewBag.PendingCars = allCars.Count(c => c.Status == CarStatus.PendingApproval);

                // Doanh thu
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);

                ViewBag.TodayRevenue = await _paymentService.GetTotalPaymentsAsync(today, today.AddDays(1));
                ViewBag.MonthlyRevenue = await _paymentService.GetTotalPaymentsAsync(monthStart, monthStart.AddMonths(1));

                // Biểu đồ doanh thu 12 tháng gần nhất
                ViewBag.MonthlyRevenueChart = await GetMonthlyRevenueDataAsync();

                // Biểu đồ trạng thái xe
                ViewBag.CarStatusChart = GetCarStatusData(allCars);

                // Thống kê chi tiết
                ViewBag.CompletedRentals = allRentals.Count(r => r.Status == RentalStatus.Completed);
                ViewBag.CancelledRentals = allRentals.Count(r => r.Status == RentalStatus.Cancelled);
                ViewBag.OverdueRentals = allRentals.Count(r => r.Status == RentalStatus.Overdue);

                // Thống kê xe theo trạng thái
                ViewBag.AvailableCars = allCars.Count(c => c.Status == CarStatus.Available);
                ViewBag.RentedCars = allCars.Count(c => c.Status == CarStatus.Rented);
                ViewBag.MaintenanceCars = allCars.Count(c => c.Status == CarStatus.Maintenance);

                // Các rental mới nhất (5 cái)
                var recentRentals = allRentals
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToList();

                // Các xe mới nhất (5 cái)
                var recentCars = allCars
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList();

                // Truyền text helpers qua ViewBag
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);

                // Truyền các danh sách qua ViewBag
                ViewBag.RecentRentals = recentRentals;
                ViewBag.RecentCars = recentCars;

                // Trả về view rỗng vì tất cả dữ liệu đều trong ViewBag
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dashboard.";

                // Return empty ViewBag data in case of error
                ViewBag.TotalCars = 0;
                ViewBag.ActiveCars = 0;
                ViewBag.TotalRentals = 0;
                ViewBag.ActiveRentals = 0;
                ViewBag.PendingRentals = 0;
                ViewBag.PendingCars = 0;
                ViewBag.TodayRevenue = 0m;
                ViewBag.MonthlyRevenue = 0m;
                ViewBag.MonthlyRevenueChart = new List<object>();
                ViewBag.CarStatusChart = new List<object>();
                ViewBag.RecentRentals = new List<Rental>();
                ViewBag.RecentCars = new List<Car>();
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);

                return View();
            }
        }

        [HttpGet]
        [Route("Admin/Dashboard/MonthlyRevenueData")]
        private async Task<List<object>> GetMonthlyRevenueDataAsync()
        {
            var result = new List<object>();
            var today = DateTime.Today;

            for (int i = 11; i >= 0; i--)
            {
                var monthStart = today.AddMonths(-i).AddDays(1 - today.Day);
                var monthEnd = monthStart.AddMonths(1);

                var revenue = await _paymentService.GetTotalPaymentsAsync(monthStart, monthEnd);

                result.Add(new
                {
                    Month = monthStart.ToString("MM/yyyy"),
                    Revenue = revenue,
                    Year = monthStart.Year
                });
            }

            return result;
        }

        [HttpGet]
        [Route("Admin/Dashboard/CarStatusData")]
        private List<object> GetCarStatusData(IEnumerable<Car> cars)
        {
            var statusGroups = cars.GroupBy(c => c.Status);
            var colors = new Dictionary<CarStatus, string>
            {
                { CarStatus.Available, "#28a745" },
                { CarStatus.Rented, "#ffc107" },
                { CarStatus.Maintenance, "#dc3545" },
                { CarStatus.PendingApproval, "#6c757d" }
            };

            return statusGroups.Select(g => new
            {
                Status = GetCarStatusText(g.Key),
                Count = g.Count(),
                Color = colors.GetValueOrDefault(g.Key, "#6c757d")
            }).Cast<object>().ToList();
        }

        [HttpGet]
        [Route("Admin/Dashboard/CarStatusText")]
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

        [HttpGet]
        [Route("Admin/Dashboard/RentalStatusText")]
        private string GetRentalStatusText(RentalStatus status)
        {
            return status switch
            {
                RentalStatus.Pending => "Chờ xác nhận",
                RentalStatus.Confirmed => "Đã xác nhận",
                RentalStatus.Active => "Đang thuê",
                RentalStatus.Completed => "Hoàn thành",
                RentalStatus.Cancelled => "Đã hủy",
                RentalStatus.Overdue => "Quá hạn",
                _ => "Không xác định"
            };
        }

        // API endpoint để lấy dữ liệu biểu đồ (nếu cần cho AJAX)
        [HttpGet]
        [Route("Admin/Dashboard/RevenueChartData")]
        public async Task<JsonResult> GetRevenueChartData()
        {
            try
            {
                var data = await GetMonthlyRevenueDataAsync();
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue chart data");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        [Route("Admin/Dashboard/CarStatusChartData")]
        public async Task<JsonResult> GetCarStatusChartData()
        {
            try
            {
                var allCars = await _carService.GetAllCarsAsync();
                var data = GetCarStatusData(allCars);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting car status chart data");
                return Json(new List<object>());
            }
        }
    }
}