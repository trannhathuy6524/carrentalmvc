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
    public class RentalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RentalsController> _logger;

        public RentalsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<RentalsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Driver/Rentals
        public async Task<IActionResult> Index(RentalStatus? status, string? filter)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                // ✅ PHASE 2: Lấy cả đơn available (chưa có driver) + đơn đã assign
                IQueryable<Rental> query;

                if (filter == "available")
                {
                    // ✅ Đơn available: RequiresDriver = true, DriverId = NULL, Status = Pending
                    // Và driver này thuộc về car owner của xe đó
                    var driverAssignments = await _context.DriverAssignments
                        .Where(da => da.DriverId == user.Id && da.IsActive)
                        .Select(da => da.CarOwnerId)
                        .ToListAsync();

                    query = _context.Rentals
                        .Include(r => r.Car)
                            .ThenInclude(c => c.Owner)
                        .Include(r => r.Car)
                            .ThenInclude(c => c.Brand)
                        .Include(r => r.Renter)
                        .Where(r => r.RequiresDriver && 
                                    r.DriverId == null && 
                                    r.Status == RentalStatus.Pending &&
                                    driverAssignments.Contains(r.Car.OwnerId));
                }
                else
                {
                    // ✅ Đơn đã assign cho driver này
                    query = _context.Rentals
                        .Include(r => r.Car)
                            .ThenInclude(c => c.Owner)
                        .Include(r => r.Car)
                            .ThenInclude(c => c.Brand)
                        .Include(r => r.Renter)
                        .Where(r => r.DriverId == user.Id);

                    // Filter by status
                    if (status.HasValue)
                    {
                        query = query.Where(r => r.Status == status.Value);
                    }

                    // Filter by driver acceptance
                    if (!string.IsNullOrEmpty(filter))
                    {
                        switch (filter.ToLower())
                        {
                            case "pending":
                                query = query.Where(r => !r.DriverAccepted.HasValue);
                                break;
                            case "accepted":
                                query = query.Where(r => r.DriverAccepted == true);
                                break;
                            case "rejected":
                                query = query.Where(r => r.DriverAccepted == false);
                                break;
                        }
                    }
                }

                var rentals = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                ViewBag.CurrentStatus = status;
                ViewBag.CurrentFilter = filter;

                return View(rentals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver rentals");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn thuê.";
                return View(new List<Rental>());
            }
        }

        // GET: Driver/Rentals/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _context.Rentals
                    .Include(r => r.Car)
                        .ThenInclude(c => c.Owner)
                    .Include(r => r.Car)
                        .ThenInclude(c => c.Brand)
                    .Include(r => r.Car)
                        .ThenInclude(c => c.Category)
                    .Include(r => r.Car)
                        .ThenInclude(c => c.CarImages)
                    .Include(r => r.Renter)
                    .Include(r => r.Payments)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê.";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ FIX: Check permissions
                // Driver có thể xem nếu:
                // 1. Đơn đã assign cho họ (rental.DriverId == user.Id), HOẶC
                // 2. Đơn available và driver này thuộc về car owner của xe đó
                
                bool canView = false;
                
                if (rental.DriverId == user!.Id)
                {
                    // Case 1: Đơn đã assign cho driver này
                    canView = true;
                }
                else if (rental.RequiresDriver && rental.DriverId == null && rental.Status == RentalStatus.Pending)
                {
                    // Case 2: Đơn available - kiểm tra driver có quyền nhận không
                    var hasPermission = await _context.DriverAssignments
                        .AnyAsync(da => da.DriverId == user.Id && 
                                       da.CarOwnerId == rental.Car.OwnerId && 
                                       da.IsActive);
                    canView = hasPermission;
                }

                if (!canView)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xem đơn thuê này.";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ Check permissions for actions
                var isAvailable = rental.RequiresDriver && rental.DriverId == null && rental.Status == RentalStatus.Pending;
                var canClaim = isAvailable; // Có thể nhận nếu là đơn available
                var canAccept = rental.DriverId == user.Id && rental.Status == RentalStatus.Confirmed && !rental.DriverAccepted.HasValue;
                var canReject = rental.DriverId == user.Id && rental.Status == RentalStatus.Confirmed && !rental.DriverAccepted.HasValue;
                var isActive = rental.Status == RentalStatus.Active;

                ViewBag.IsAvailable = isAvailable;
                ViewBag.CanClaim = canClaim;
                ViewBag.CanAccept = canAccept;
                ViewBag.CanReject = canReject;
                ViewBag.IsActive = isActive;

                return View(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rental details: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn thuê.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Driver/Rentals/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _context.Rentals
                    .Include(r => r.Car)
                    .FirstOrDefaultAsync(r => r.RentalId == id && r.DriverId == user!.Id);

                if (rental == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Status != RentalStatus.Confirmed)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể chấp nhận đơn ở trạng thái 'Đã xác nhận'.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (rental.DriverAccepted.HasValue)
                {
                    TempData["ErrorMessage"] = "Đơn này đã được xử lý rồi.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Accept the rental
                rental.DriverAccepted = true;
                rental.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã chấp nhận đơn thuê xe thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi chấp nhận đơn thuê.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Driver/Rentals/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _context.Rentals
                    .FirstOrDefaultAsync(r => r.RentalId == id && r.DriverId == user!.Id);

                if (rental == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Status != RentalStatus.Confirmed)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể từ chối đơn ở trạng thái 'Đã xác nhận'.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (rental.DriverAccepted.HasValue)
                {
                    TempData["ErrorMessage"] = "Đơn này đã được xử lý rồi.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Reject the rental
                rental.DriverAccepted = false;
                rental.UpdatedAt = DateTime.UtcNow;
                
                // Optionally store reason in Notes
                if (!string.IsNullOrEmpty(reason))
                {
                    rental.Notes = (rental.Notes ?? "") + $"\n[Tài xế từ chối: {reason}]";
                }

                // Clear driver assignment
                rental.DriverId = null;
                rental.DriverAssignedAt = null;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã từ chối đơn thuê xe. Chủ xe sẽ được thông báo.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi từ chối đơn thuê.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ✅ PHASE 2: POST: Driver/Rentals/Claim/5
        // Driver nhận đơn available (first-come-first-served)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Claim(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                // 1. Kiểm tra driver có quyền nhận đơn này không
                var driverAssignment = await _context.DriverAssignments
                    .Include(da => da.CarOwner)
                    .FirstOrDefaultAsync(da => da.DriverId == user.Id && da.IsActive);

                if (driverAssignment == null)
                {
                    TempData["ErrorMessage"] = "Bạn chưa được phân công làm tài xế cho chủ xe nào.";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Lấy rental với lock (prevent race condition)
                var rental = await _context.Rentals
                    .Include(r => r.Car)
                    .FirstOrDefaultAsync(r => r.RentalId == id);

                if (rental == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê.";
                    return RedirectToAction(nameof(Index));
                }

                // 3. Validate
                if (!rental.RequiresDriver)
                {
                    TempData["ErrorMessage"] = "Đơn này không yêu cầu tài xế.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.DriverId != null)
                {
                    TempData["ErrorMessage"] = "Đơn này đã có tài xế khác nhận rồi.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Status != RentalStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể nhận đơn ở trạng thái 'Chờ xác nhận'.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Car.OwnerId != driverAssignment.CarOwnerId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền nhận đơn này.";
                    return RedirectToAction(nameof(Index));
                }

                // 4. ✅ Claim the rental (First-come-first-served)
                rental.DriverId = user.Id;
                rental.DriverAssignedAt = DateTime.UtcNow;
                rental.DriverAccepted = true; // ✅ Tự động accept khi claim
                rental.ActualDriverFee = driverAssignment.DailyDriverFee; // ✅ Lưu actual fee
                rental.UpdatedAt = DateTime.UtcNow;

                // 5. ✅ Cập nhật notes
                var duration = rental.EndDate - rental.StartDate;
                var totalDays = Math.Max(1, (int)Math.Ceiling(duration.TotalDays));
                var totalHours = (decimal)Math.Ceiling(duration.TotalHours);
                var isHourly = totalHours < 24 && totalHours >= 4;

                var actualFee = isHourly 
                    ? totalHours * (driverAssignment.DailyDriverFee / 8)
                    : totalDays * driverAssignment.DailyDriverFee;

                var claimNote = $"\n\n** TÀI XẾ NHẬN ĐƠN **\n";
                claimNote += $"Tài xế: {user.FullName ?? user.Email}\n";
                claimNote += $"Phí ước tính: {rental.EstimatedDriverFee:N0} VNĐ/ngày\n";
                claimNote += $"Phí thực tế: {driverAssignment.DailyDriverFee:N0} VNĐ/ngày\n";
                claimNote += $"Tổng phí tài xế: {actualFee:N0} VNĐ";
                rental.Notes += claimNote;

                // 6. ✅ Cập nhật TotalPrice nếu actual fee khác estimated
                if (rental.EstimatedDriverFee.HasValue && rental.ActualDriverFee.HasValue)
                {
                    var estimatedTotal = isHourly 
                        ? totalHours * (rental.EstimatedDriverFee.Value / 8)
                        : totalDays * rental.EstimatedDriverFee.Value;
                    
                    var diff = actualFee - estimatedTotal;
                    
                    if (Math.Abs(diff) > 0.01m) // Có chênh lệch
                    {
                        rental.TotalPrice += diff; // Adjust total price
                        claimNote = $"\nĐiều chỉnh tổng giá: {(diff > 0 ? "+" : "")}{diff:N0} VNĐ (do phí tài xế thực tế khác ước tính)";
                        rental.Notes += claimNote;
                        
                        _logger.LogInformation(
                            "Rental {RentalId}: Adjusted total price by {Diff} VNĐ (Estimated: {Est}, Actual: {Act})",
                            id, diff, estimatedTotal, actualFee);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Driver {DriverId} claimed rental {RentalId} with fee {Fee}",
                    user.Id, id, driverAssignment.DailyDriverFee);

                TempData["SuccessMessage"] = $"Đã nhận đơn thuê thành công! Phí tài xế: {actualFee:N0} VNĐ";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateConcurrencyException)
            {
                // Race condition: Another driver claimed it first
                TempData["ErrorMessage"] = "Đơn này đã có tài xế khác nhận rồi. Vui lòng chọn đơn khác.";
                return RedirectToAction(nameof(Index), new { filter = "available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi nhận đơn thuê.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
