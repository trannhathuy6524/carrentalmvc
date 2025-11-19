using carrentalmvc.Data;
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace carrentalmvc.Controllers
{
    public class CarOwnerRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CarOwnerRequestController> _logger;

        public CarOwnerRequestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CarOwnerRequestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: CarOwnerRequest/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Check if user already is a car owner
            if (await _userManager.IsInRoleAsync(user, RoleConstants.Owner))
            {
                TempData["ErrorMessage"] = "Bạn đã là chủ xe rồi!";
                return RedirectToAction("Index", "Dashboard", new { area = "CarOwner" });
            }

            // Check if user has pending request
            var existingRequest = await _context.CarOwnerRequests
                .FirstOrDefaultAsync(r => r.UserId == user.Id && r.Status == CarOwnerRequestStatus.Pending);

            if (existingRequest != null)
            {
                TempData["ErrorMessage"] = "Bạn đã có yêu cầu đang chờ duyệt.";
                return RedirectToAction(nameof(Status));
            }

            // Pre-fill user info
            var model = new CarOwnerRequestViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                NationalId = user.NationalId ?? "",
                Address = user.Address ?? ""
            };

            return View(model);
        }

        // POST: CarOwnerRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(CarOwnerRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Double check not already car owner
            if (await _userManager.IsInRoleAsync(user, RoleConstants.Owner))
            {
                TempData["ErrorMessage"] = "Bạn đã là chủ xe rồi!";
                return RedirectToAction("Index", "Dashboard", new { area = "CarOwner" });
            }

            // Create request
            var request = new CarOwnerRequest
            {
                UserId = user.Id,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                NationalId = model.NationalId,
                Address = model.Address,
                ExpectedCarCount = model.ExpectedCarCount,
                Experience = model.Experience,
                Notes = model.Notes,
                Status = CarOwnerRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.CarOwnerRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gửi yêu cầu thành công! Chúng tôi sẽ xem xét và phản hồi sớm nhất.";
            _logger.LogInformation("User {UserId} submitted car owner request", user.Id);

            return RedirectToAction(nameof(Status));
        }

        // GET: CarOwnerRequest/Status
        [Authorize]
        public async Task<IActionResult> Status()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var requests = await _context.CarOwnerRequests
                .Include(r => r.Processor)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return View(requests);
        }
    }

    // ViewModel for Create form
    public class CarOwnerRequestViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số CCCD/CMND")]
        [StringLength(12, MinimumLength = 9, ErrorMessage = "CCCD/CMND phải từ 9-12 số")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số xe dự kiến")]
        [Range(1, 100, ErrorMessage = "Số xe phải từ 1-100")]
        [Display(Name = "Số xe dự kiến cho thuê")]
        public int ExpectedCarCount { get; set; } = 1;

        [StringLength(500)]
        [Display(Name = "Kinh nghiệm cho thuê xe")]
        public string? Experience { get; set; }

        [StringLength(1000)]
        [Display(Name = "Ghi chú thêm")]
        public string? Notes { get; set; }
    }
}
