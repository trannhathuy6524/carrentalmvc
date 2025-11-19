using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Areas.Driver.Controllers
{
    [Area("Driver")]
    [Authorize(Roles = RoleConstants.Driver)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Driver/Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                // ✅ PHASE 2: Lấy đơn available (chưa có driver)
                var driverAssignments = await _context.DriverAssignments
                    .Where(da => da.DriverId == user.Id && da.IsActive)
                    .Select(da => da.CarOwnerId)
                    .ToListAsync();

                var availableRentals = await _context.Rentals
                    .Include(r => r.Car)
                        .ThenInclude(c => c.Owner)
                    .Include(r => r.Car)
                        .ThenInclude(c => c.Brand)
                    .Include(r => r.Renter)
                    .Where(r => r.RequiresDriver && 
                                r.DriverId == null && 
                                r.Status == RentalStatus.Pending &&
                                driverAssignments.Contains(r.Car.OwnerId))
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                // Lấy tất cả rentals được gán cho driver này
                var myRentals = await _context.Rentals
                    .Include(r => r.Car)
                        .ThenInclude(c => c.Owner)
                    .Include(r => r.Car)
                        .ThenInclude(c => c.Brand)
                    .Include(r => r.Renter)
                    .Where(r => r.DriverId == user.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                // Thống kê
                var stats = new
                {
                    // ✅ PHASE 2: Thêm available count
                    AvailableRentals = availableRentals.Count,
                    
                    TotalAssigned = myRentals.Count,
                    Pending = myRentals.Count(r => r.Status == RentalStatus.Confirmed && !r.DriverAccepted.HasValue),
                    Accepted = myRentals.Count(r => r.DriverAccepted == true && r.Status == RentalStatus.Confirmed),
                    Active = myRentals.Count(r => r.Status == RentalStatus.Active),
                    Completed = myRentals.Count(r => r.Status == RentalStatus.Completed),
                    Rejected = myRentals.Count(r => r.DriverAccepted == false),
                    
                    // ✅ PHASE 2: Tính thu nhập từ ActualDriverFee thay vì 30% TotalPrice
                    TotalEarnings = myRentals
                        .Where(r => r.Status == RentalStatus.Completed && r.ActualDriverFee.HasValue)
                        .Sum(r => r.ActualDriverFee.Value),
                    
                    ThisMonthEarnings = myRentals
                        .Where(r => r.Status == RentalStatus.Completed && 
                                   r.ActualDriverFee.HasValue &&
                                   r.ReturnDate.HasValue &&
                                   r.ReturnDate.Value.Month == DateTime.Now.Month)
                        .Sum(r => r.ActualDriverFee.Value)
                };

                ViewBag.Stats = stats;

                // ✅ PHASE 2: Pass available rentals
                ViewBag.AvailableRentals = availableRentals.Take(5).ToList();

                // Đơn cần xử lý (chưa accept/reject)
                var pendingRentals = myRentals
                    .Where(r => r.Status == RentalStatus.Confirmed && !r.DriverAccepted.HasValue)
                    .Take(5)
                    .ToList();

                ViewBag.PendingRentals = pendingRentals;

                // Đơn đang active
                var activeRentals = myRentals
                    .Where(r => r.Status == RentalStatus.Active)
                    .ToList();

                ViewBag.ActiveRentals = activeRentals;

                // Đơn sắp tới (đã accept, chưa bắt đầu)
                var upcomingRentals = myRentals
                    .Where(r => r.DriverAccepted == true && 
                               r.Status == RentalStatus.Confirmed &&
                               r.StartDate > DateTime.Now)
                    .OrderBy(r => r.StartDate)
                    .Take(5)
                    .ToList();

                ViewBag.UpcomingRentals = upcomingRentals;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver dashboard");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dashboard.";
                return View();
            }
        }
    }
}
