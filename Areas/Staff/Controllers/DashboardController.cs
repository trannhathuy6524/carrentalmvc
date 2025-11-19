using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Areas.Staff.Controllers
{
    [Area("Staff")]
    [Authorize(Roles = RoleConstants.Staff)]
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

        public async Task<IActionResult> Index()
        {
            try
            {
                var staff = await _userManager.GetUserAsync(User);
                ViewBag.StaffName = staff?.FullName ?? "Nhân viên";

                // Statistics
                var totalCars = await _context.Cars.CountAsync();
                var totalRentals = await _context.Rentals.CountAsync();
                var activeRentals = await _context.Rentals.CountAsync(r => r.Status == RentalStatus.Active);
                var totalPayments = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;

                // Recent rentals (last 10)
                var recentRentals = await _context.Rentals
                    .Include(r => r.Car)
                    .Include(r => r.Renter)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                ViewBag.TotalCars = totalCars;
                ViewBag.TotalRentals = totalRentals;
                ViewBag.ActiveRentals = activeRentals;
                ViewBag.TotalPayments = totalPayments;
                ViewBag.RecentRentals = recentRentals;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Staff dashboard");
                TempData["ErrorMessage"] = "Có lỗi khi tải dashboard.";
                return View();
            }
        }
    }
}
