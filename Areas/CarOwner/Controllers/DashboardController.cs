using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Models.ViewModels;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;



namespace carrentalmvc.Areas.CarOwner.Controllers
{
    [Area("CarOwner")]
    [Authorize(Roles = RoleConstants.Owner)]
    public class DashboardController : Controller
    {
        private readonly ICarService _carService;
        private readonly IRentalService _rentalService;
        private readonly IPaymentService _paymentService;
        private readonly IReviewService _reviewService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ICarService carService,
            IRentalService rentalService,
            IPaymentService paymentService,
            IReviewService reviewService,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _carService = carService;
            _rentalService = rentalService;
            _paymentService = paymentService;
            _reviewService = reviewService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Lấy dữ liệu cơ bản của chủ xe
                var myCars = await _carService.GetCarsByOwnerAsync(user.Id);
                var myRentals = await _rentalService.GetRentalsByOwnerAsync(user.Id);
                var activeRentals = myRentals.Where(r => r.Status == RentalStatus.Active);

                // Thống kê xe
                ViewBag.MyCarsCount = myCars.Count();
                ViewBag.AvailableCars = myCars.Count(c => c.Status == CarStatus.Available && c.IsActive);
                ViewBag.CarsInRental = myCars.Count(c => c.Status == CarStatus.Rented);

                // Thống kê rental
                ViewBag.TotalRentals = myRentals.Count();
                ViewBag.ActiveRentals = activeRentals.Count();

                // ✅ THÊM: Calculate revenue breakdown
                var revenueStats = await CalculateRevenueStatsAsync(user.Id, myRentals);
                ViewBag.RevenueStats = revenueStats;

                // Tính doanh thu tháng này (giữ lại cho compatibility)
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                ViewBag.MonthlyIncome = await _paymentService.GetTotalPaymentsByOwnerAsync(user.Id, monthStart, monthStart.AddMonths(1));

                // Tính đánh giá trung bình
                var allReviews = new List<Review>();
                foreach (var car in myCars)
                {
                    var carReviews = await _reviewService.GetReviewsByCarAsync(car.CarId);
                    allReviews.AddRange(carReviews);
                }
                ViewBag.AverageRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0.0;
                ViewBag.TotalReviews = allReviews.Count;

                // Biểu đồ doanh thu 6 tháng gần nhất
                ViewBag.MonthlyIncomeChart = await GetMonthlyIncomeChartAsync(user.Id);

                // ✅ THÊM: Monthly revenue breakdown chart
                ViewBag.MonthlyRevenueBreakdown = await GetMonthlyRevenueBreakdownAsync(user.Id);

                // Danh sách xe mới nhất (tối đa 5)
                var recentCars = myCars
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList();

                // Danh sách rental gần đây (tối đa 5)
                var recentRentals = myRentals
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToList();

                // Helper functions for view
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);

                // Truyền dữ liệu qua ViewBag
                ViewBag.RecentCars = recentCars;
                ViewBag.RecentRentals = recentRentals;

                // Model chính là user
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car owner dashboard for user: {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dashboard.";

                // Return empty data
                ViewBag.MyCarsCount = 0;
                ViewBag.AvailableCars = 0;
                ViewBag.CarsInRental = 0;
                ViewBag.TotalRentals = 0;
                ViewBag.ActiveRentals = 0;
                ViewBag.MonthlyIncome = 0m;
                ViewBag.RevenueStats = new OwnerRevenueStats();
                ViewBag.AverageRating = 0.0;
                ViewBag.TotalReviews = 0;
                ViewBag.MonthlyIncomeChart = new List<MonthlyIncomeData>();
                ViewBag.MonthlyRevenueBreakdown = new List<MonthlyRevenue>();
                ViewBag.RecentCars = new List<Car>();
                ViewBag.RecentRentals = new List<Rental>();
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);

                return View(new ApplicationUser());
            }
        }

        /// <summary>
        /// ✅ THÊM: Calculate comprehensive revenue statistics
        /// </summary>
        private async Task<OwnerRevenueStats> CalculateRevenueStatsAsync(string ownerId, IEnumerable<Rental> allRentals)
        {
            var completedRentals = allRentals.Where(r => r.Status == RentalStatus.Completed).ToList();
            var activeRentals = allRentals.Where(r => r.Status == RentalStatus.Active).ToList();

            var stats = new OwnerRevenueStats
            {
                CompletedRentals = completedRentals.Count,
                ActiveRentals = activeRentals.Count,
                CommissionRate = PlatformConstants.COMMISSION_RATE
            };

            // Calculate from completed rentals
            foreach (var rental in completedRentals)
            {
                var totalPrice = rental.TotalPrice;
                var driverFee = rental.ActualDriverFee ?? 0m;

                // Get breakdown
                var breakdown = PlatformConstants.GetRevenueBreakdown(totalPrice, driverFee);

                stats.TotalBookingValue += totalPrice;
                stats.PlatformCommission += breakdown.PlatformFee;
                stats.DriverFees += driverFee;
                stats.NetRevenue += breakdown.OwnerRevenue;

                // Calculate rental and delivery revenue
                // RentalRevenue = TotalPrice - DriverFee - DeliveryFee
                // For simplicity, we'll calculate delivery as: TotalPrice - RentalDays * PricePerDay - DriverFee
                var days = Math.Max(1, (int)Math.Ceiling((rental.EndDate - rental.StartDate).TotalDays));
                var estimatedRentalCost = days * (rental.Car?.PricePerDay ?? 0);
                var estimatedDeliveryFee = totalPrice - estimatedRentalCost - driverFee;

                stats.RentalRevenue += estimatedRentalCost;
                stats.DeliveryRevenue += Math.Max(0, estimatedDeliveryFee); // Ensure non-negative
            }

            // Get payment statistics
            var allPayments = new List<Payment>();
            foreach (var rental in completedRentals)
            {
                var payments = await _paymentService.GetPaymentsByRentalAsync(rental.RentalId);
                allPayments.AddRange(payments);
            }

            stats.CompletedPayments = allPayments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
            stats.PendingPayments = allPayments.Where(p => p.Status == PaymentStatus.Pending).Sum(p => p.Amount);

            return stats;
        }

        /// <summary>
        /// ✅ THÊM: Get monthly revenue breakdown for last 6 months
        /// </summary>
        private async Task<List<MonthlyRevenue>> GetMonthlyRevenueBreakdownAsync(string ownerId)
        {
            var result = new List<MonthlyRevenue>();
            var today = DateTime.Today;

            for (int i = 5; i >= 0; i--)
            {
                var monthStart = today.AddMonths(-i).AddDays(1 - today.Day);
                var monthEnd = monthStart.AddMonths(1);

                // Get rentals for this month
                var allRentals = await _rentalService.GetRentalsByOwnerAsync(ownerId);
                var monthRentals = allRentals.Where(r => 
                    r.Status == RentalStatus.Completed &&
                    r.EndDate >= monthStart &&
                    r.EndDate < monthEnd).ToList();

                var monthData = new MonthlyRevenue
                {
                    Year = monthStart.Year,
                    Month = monthStart.Month,
                    MonthName = monthStart.ToString("MM/yyyy"),
                    RentalCount = monthRentals.Count
                };

                foreach (var rental in monthRentals)
                {
                    var driverFee = rental.ActualDriverFee ?? 0m;
                    var breakdown = PlatformConstants.GetRevenueBreakdown(rental.TotalPrice, driverFee);

                    monthData.TotalBookingValue += rental.TotalPrice;
                    monthData.PlatformCommission += breakdown.PlatformFee;
                    monthData.DriverFees += driverFee;
                    monthData.NetRevenue += breakdown.OwnerRevenue;
                }

                result.Add(monthData);
            }

            return result;
        }

        private async Task<List<MonthlyIncomeData>> GetMonthlyIncomeChartAsync(string ownerId)
        {
            var result = new List<MonthlyIncomeData>();
            var today = DateTime.Today;

            for (int i = 5; i >= 0; i--)
            {
                var monthStart = today.AddMonths(-i).AddDays(1 - today.Day);
                var monthEnd = monthStart.AddMonths(1);

                var income = await _paymentService.GetTotalPaymentsByOwnerAsync(ownerId, monthStart, monthEnd);

                result.Add(new MonthlyIncomeData
                {
                    Month = monthStart.ToString("MM/yyyy"),
                    Income = income,
                    Year = monthStart.Year,
                    MonthNumber = monthStart.Month
                });
            }

            return result;
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
    }

    // Simple class for monthly income chart data (not a ViewModel, just a data structure)
    public class MonthlyIncomeData
    {
        public string Month { get; set; }
        public decimal Income { get; set; }
        public int Year { get; set; }
        public int MonthNumber { get; set; }
    }
}