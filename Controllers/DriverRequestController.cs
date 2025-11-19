using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Controllers
{
    [Authorize]
    public class DriverRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DriverRequestController> _logger;

        public DriverRequestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DriverRequestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: DriverRequest/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Kiểm tra đã là driver chưa
            if (await _userManager.IsInRoleAsync(user, RoleConstants.Driver))
            {
                TempData["ErrorMessage"] = "Bạn đã là tài xế rồi!";
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra có request pending không
            var hasPendingRequest = await _context.DriverRequests
                .AnyAsync(dr => dr.UserId == user.Id && dr.Status == DriverRequestStatus.Pending);

            if (hasPendingRequest)
            {
                TempData["ErrorMessage"] = "Bạn đã có yêu cầu đang chờ duyệt.";
                return RedirectToAction(nameof(Status));
            }

            // Load danh sách CarOwner
            var carOwners = await _userManager.GetUsersInRoleAsync(RoleConstants.Owner);
            ViewBag.CarOwners = carOwners
                .Where(o => o.IsActive)
                .Select(o => new SelectListItem
                {
                    Value = o.Id,
                    Text = $"{o.FullName ?? o.Email} - {o.PhoneNumber ?? "Chưa có SĐT"}"
                })
                .ToList();

            // Pre-fill user info
            var model = new DriverRequestViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                NationalId = user.NationalId ?? "",
                DriverLicense = user.DriverLicense ?? ""
                // ❌ REMOVED: RequestedDailyFee = 300000m (không cần nữa)
            };

            return View(model);
        }

        // POST: DriverRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DriverRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadCarOwnersAsync();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Validate CarOwner
            var carOwner = await _userManager.FindByIdAsync(model.CarOwnerId);
            if (carOwner == null || !await _userManager.IsInRoleAsync(carOwner, RoleConstants.Owner))
            {
                ModelState.AddModelError("", "Chủ xe không hợp lệ.");
                await LoadCarOwnersAsync();
                return View(model);
            }

            // Create request
            var request = new DriverRequest
            {
                UserId = user.Id,
                CarOwnerId = model.CarOwnerId,
                DriverLicense = model.DriverLicense,
                NationalId = model.NationalId,
                Experience = model.Experience,
                Notes = model.Notes,
                Status = DriverRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            // Update user info if needed
            if (string.IsNullOrEmpty(user.NationalId))
                user.NationalId = model.NationalId;
            
            if (string.IsNullOrEmpty(user.DriverLicense))
                user.DriverLicense = model.DriverLicense;

            _context.DriverRequests.Add(request);
            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Driver request created: UserId={UserId}, OwnerId={OwnerId}", 
                user.Id, model.CarOwnerId);

            TempData["SuccessMessage"] = "Yêu cầu làm tài xế đã được gửi! Lương tài xế: 500,000 VNĐ/ngày. Vui lòng chờ chủ xe xác nhận.";
            return RedirectToAction(nameof(Status));
        }

        // GET: DriverRequest/Status
        public async Task<IActionResult> Status()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var requests = await _context.DriverRequests
                .Include(dr => dr.CarOwner)
                .Where(dr => dr.UserId == user.Id)
                .OrderByDescending(dr => dr.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        private async Task LoadCarOwnersAsync()
        {
            var carOwners = await _userManager.GetUsersInRoleAsync(RoleConstants.Owner);
            ViewBag.CarOwners = carOwners
                .Where(o => o.IsActive)
                .Select(o => new SelectListItem
                {
                    Value = o.Id,
                    Text = $"{o.FullName ?? o.Email} - {o.PhoneNumber ?? "Chưa có SĐT"}"
                })
                .ToList();
        }
    }

    // ViewModel
    public class DriverRequestViewModel
    {
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn chủ xe")]
        [Display(Name = "Chọn chủ xe")]
        public string CarOwnerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "CCCD/CMND là bắt buộc")]
        [StringLength(20)]
        [Display(Name = "Số CCCD/CMND")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "GPLX là bắt buộc")]
        [StringLength(20)]
        [Display(Name = "Số giấy phép lái xe (GPLX)")]
        public string DriverLicense { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Kinh nghiệm lái xe")]
        [DataType(DataType.MultilineText)]
        public string? Experience { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú thêm")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        // ❌ REMOVED: RequestedDailyFee field
        // Lương cố định 500,000 VNĐ/ngày
    }
}