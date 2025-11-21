using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class CarOwnerRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<CarOwnerRequestsController> _logger;

        public CarOwnerRequestsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<CarOwnerRequestsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: Admin/CarOwnerRequests
        [HttpGet]
        [Route("Admin/CarOwnerRequests")]
        public async Task<IActionResult> Index(CarOwnerRequestStatus? status)
        {
            var query = _context.CarOwnerRequests
                .Include(r => r.User)
                .Include(r => r.Processor)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            ViewBag.Status = status;
            return View(requests);
        }

        // GET: Admin/CarOwnerRequests/Details/5
        [HttpGet]
        [Route("Admin/CarOwnerRequests/Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.CarOwnerRequests
                .Include(r => r.User)
                .Include(r => r.Processor)
                .FirstOrDefaultAsync(r => r.CarOwnerRequestId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            return View(request);
        }

        // POST: Admin/CarOwnerRequests/Approve/5
        [HttpPost]
        [Route("Admin/CarOwnerRequests/Approve/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var admin = await _userManager.GetUserAsync(User);
            var request = await _context.CarOwnerRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.CarOwnerRequestId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Status != CarOwnerRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Yêu cầu này đã được xử lý rồi.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Update request status
            request.Status = CarOwnerRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = admin?.Id;

            var user = request.User;

            // ✅ REMOVE CUSTOMER ROLE nếu có
            if (await _userManager.IsInRoleAsync(user, RoleConstants.Customer))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, RoleConstants.Customer);
                if (removeResult.Succeeded)
                {
                    _logger.LogInformation("Removed Customer role from user {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to remove Customer role from user {UserId}: {Errors}", 
                        user.Id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                }
            }

            // ✅ ADD CAR OWNER ROLE
            if (!await _userManager.IsInRoleAsync(user, RoleConstants.Owner))
            {
                var addResult = await _userManager.AddToRoleAsync(user, RoleConstants.Owner);
                if (addResult.Succeeded)
                {
                    _logger.LogInformation("Added CarOwner role to user {UserId}", user.Id);
                }
                else
                {
                    _logger.LogError("Failed to add CarOwner role to user {UserId}: {Errors}", 
                        user.Id, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Có lỗi khi thêm quyền chủ xe. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã duyệt yêu cầu của {request.FullName}. Người dùng đã trở thành chủ xe.";
            _logger.LogInformation("Admin {AdminId} approved car owner request {RequestId}", admin?.Id, id);

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/CarOwnerRequests/Reject/5
        [HttpPost]
        [Route("Admin/CarOwnerRequests/Reject/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var admin = await _userManager.GetUserAsync(User);
            var request = await _context.CarOwnerRequests
                .FirstOrDefaultAsync(r => r.CarOwnerRequestId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Status != CarOwnerRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Yêu cầu này đã được xử lý rồi.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.Status = CarOwnerRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = admin?.Id;
            request.RejectionReason = reason ?? "Không đáp ứng yêu cầu";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã từ chối yêu cầu của {request.FullName}.";
            _logger.LogInformation("Admin {AdminId} rejected car owner request {RequestId}, Reason: {Reason}",
                admin?.Id, id, reason);

            return RedirectToAction(nameof(Index));
        }
    }
}
