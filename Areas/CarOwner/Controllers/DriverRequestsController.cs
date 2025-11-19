using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Areas.CarOwner.Controllers
{
    [Area("CarOwner")]
    [Authorize(Roles = RoleConstants.Owner)]
    public class DriverRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DriverRequestsController> _logger;

        public DriverRequestsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DriverRequestsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: CarOwner/DriverRequests
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var requests = await _context.DriverRequests
                .Include(dr => dr.User)
                .Where(dr => dr.CarOwnerId == user.Id)
                .OrderBy(dr => dr.Status)
                .ThenByDescending(dr => dr.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        // POST: CarOwner/DriverRequests/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var request = await _context.DriverRequests
                .Include(dr => dr.User)
                .FirstOrDefaultAsync(dr => dr.DriverRequestId == id && dr.CarOwnerId == user!.Id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Status != DriverRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Yêu cầu đã được xử lý rồi.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ FIXED SALARY: 500,000 VNĐ/ngày
            const decimal FIXED_DRIVER_FEE = 500000m;

            // Update request status
            request.Status = DriverRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = user!.Id;

            var driver = request.User;

            // ✅ REMOVE CUSTOMER ROLE nếu có
            if (await _userManager.IsInRoleAsync(driver, RoleConstants.Customer))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(driver, RoleConstants.Customer);
                if (removeResult.Succeeded)
                {
                    _logger.LogInformation("Removed Customer role from user {UserId}", driver.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to remove Customer role from user {UserId}: {Errors}", 
                        driver.Id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                }
            }

            // ✅ ADD DRIVER ROLE
            if (!await _userManager.IsInRoleAsync(driver, RoleConstants.Driver))
            {
                var addResult = await _userManager.AddToRoleAsync(driver, RoleConstants.Driver);
                if (addResult.Succeeded)
                {
                    _logger.LogInformation("Added Driver role to user {UserId}", driver.Id);
                }
                else
                {
                    _logger.LogError("Failed to add Driver role to user {UserId}: {Errors}", 
                        driver.Id, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Có lỗi khi thêm quyền tài xế. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // ✅ CREATE ASSIGNMENT WITH FIXED FEE (500K)
            var assignment = new DriverAssignment
            {
                CarOwnerId = user.Id,
                DriverId = request.UserId,
                DailyDriverFee = FIXED_DRIVER_FEE, // ← 500,000 VNĐ/ngày
                IsActive = true,
                AssignedAt = DateTime.UtcNow,
                Notes = $"Lương cố định: {FIXED_DRIVER_FEE:N0} VNĐ/ngày"
            };

            _context.DriverAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã duyệt tài xế {driver.FullName ?? driver.Email}. Lương: {FIXED_DRIVER_FEE:N0} VNĐ/ngày";
            _logger.LogInformation("Approved driver {DriverId} for owner {OwnerId} with fixed fee {Fee}",
                request.UserId, user.Id, FIXED_DRIVER_FEE);

            return RedirectToAction(nameof(MyDrivers));
        }

        // POST: CarOwner/DriverRequests/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            var request = await _context.DriverRequests
                .FirstOrDefaultAsync(dr => dr.DriverRequestId == id && dr.CarOwnerId == user!.Id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = DriverRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = user!.Id;
            request.RejectionReason = reason ?? "Không đáp ứng yêu cầu";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã từ chối yêu cầu.";
            _logger.LogInformation("Rejected driver request {RequestId}, Reason: {Reason}",
                id, reason);

            return RedirectToAction(nameof(Index));
        }

        // GET: CarOwner/DriverRequests/MyDrivers (nếu chưa có)
        public async Task<IActionResult> MyDrivers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var drivers = await _context.DriverAssignments
                .Include(da => da.Driver)
                .Where(da => da.CarOwnerId == user.Id)
                .OrderByDescending(da => da.IsActive)
                .ThenByDescending(da => da.AssignedAt)
                .ToListAsync();

            return View(drivers);
        }
    }
}