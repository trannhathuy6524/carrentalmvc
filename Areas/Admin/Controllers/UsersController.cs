
using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(
            UserType? userType,
            bool? isActive,
            bool? isVerified,
            string? searchTerm,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var allUsers = _userManager.Users.AsQueryable();

                // Apply filters
                if (userType.HasValue)
                {
                    allUsers = allUsers.Where(u => u.UserType == userType.Value);
                }

                if (isActive.HasValue)
                {
                    allUsers = allUsers.Where(u => u.IsActive == isActive.Value);
                }

                if (isVerified.HasValue)
                {
                    allUsers = allUsers.Where(u => u.IsVerified == isVerified.Value);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    allUsers = allUsers.Where(u =>
                        (u.FullName != null && u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (u.Email != null && u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = allUsers.Count();
                var pagedUsers = allUsers
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Sử dụng ViewBag để truyền dữ liệu filter và phân trang thay vì ViewModel
                ViewBag.UserType = userType;
                ViewBag.IsActive = isActive;
                ViewBag.IsVerified = isVerified;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Thống kê tổng quan
                ViewBag.TotalActiveUsers = _userManager.Users.Count(u => u.IsActive);
                ViewBag.TotalVerifiedUsers = _userManager.Users.Count(u => u.IsVerified);
                ViewBag.TotalCustomers = _userManager.Users.Count(u => u.UserType == UserType.Customer);
                ViewBag.TotalOwners = _userManager.Users.Count(u => u.UserType == UserType.Owner);
                ViewBag.TotalAdmins = _userManager.Users.Count(u => u.UserType == UserType.Admin);

                return View(pagedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách người dùng.";
                return View(new List<ApplicationUser>());
            }
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền thông tin bổ sung qua ViewBag
                ViewBag.UserTypeText = GetUserTypeText(user.UserType);
                ViewBag.TotalCars = user.Cars?.Count ?? 0;
                ViewBag.TotalRentals = user.Rentals?.Count ?? 0;
                ViewBag.TotalReviews = user.Reviews?.Count ?? 0;

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin người dùng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Users/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var statusText = user.IsActive ? "kích hoạt" : "vô hiệu hóa";
                    TempData["SuccessMessage"] = $"Đã {statusText} tài khoản thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật trạng thái tài khoản.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user active status: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái tài khoản.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Users/ToggleVerified/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVerified(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                user.IsVerified = !user.IsVerified;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var statusText = user.IsVerified ? "xác thực" : "hủy xác thực";
                    TempData["SuccessMessage"] = $"Đã {statusText} tài khoản thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật trạng thái xác thực.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user verified status: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái xác thực.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                // Truyền thông tin bổ sung qua ViewBag
                ViewBag.UserTypeText = GetUserTypeText(user.UserType);
                ViewBag.TotalCars = user.Cars?.Count ?? 0;
                ViewBag.TotalRentals = user.Rentals?.Count ?? 0;
                ViewBag.TotalReviews = user.Reviews?.Count ?? 0;
                ViewBag.HasAssociatedData = user.Cars.Any() || user.Rentals.Any() || user.Reviews.Any();

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for delete: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin người dùng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng để xóa.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if user has associated data (cars, rentals, etc.)
                if (user.Cars.Any() || user.Rentals.Any() || user.Reviews.Any())
                {
                    // Soft delete instead of hard delete
                    user.IsDeleted = true;
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.UtcNow;

                    var updateResult = await _userManager.UpdateAsync(user);
                    if (updateResult.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Tài khoản đã được vô hiệu hóa thành công (soft delete do có dữ liệu liên quan).";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không thể vô hiệu hóa tài khoản.";
                    }
                }
                else
                {
                    // Hard delete if no associated data
                    var result = await _userManager.DeleteAsync(user);
                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Tài khoản đã được xóa thành công.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không thể xóa tài khoản.";
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tài khoản.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // Helper method để chuyển đổi UserType thành text tiếng Việt
        private string GetUserTypeText(UserType userType)
        {
            return userType switch
            {
                UserType.Admin => "Quản trị viên",
                UserType.Customer => "Khách hàng",
                UserType.Owner => "Chủ xe",
                _ => "Không xác định"
            };
        }
    }
}