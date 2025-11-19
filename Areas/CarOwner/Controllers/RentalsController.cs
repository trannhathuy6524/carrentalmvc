using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using carrentalmvc.Data;

namespace carrentalmvc.Areas.CarOwner.Controllers
{
    [Area("CarOwner")]
    [Authorize(Roles = RoleConstants.Owner)]
    public class RentalsController : Controller
    {
        private readonly IRentalService _rentalService;
        private readonly ICarService _carService;
        private readonly IPaymentService _paymentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RentalsController> _logger;
        private readonly ApplicationDbContext _context;

        public RentalsController(
            IRentalService rentalService,
            ICarService carService,
            IPaymentService paymentService,
            UserManager<ApplicationUser> userManager,
            ILogger<RentalsController> logger,
            ApplicationDbContext context)
        {
            _rentalService = rentalService;
            _carService = carService;
            _paymentService = paymentService;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        // GET: CarOwner/Rentals
        public async Task<IActionResult> Index(
            RentalStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            string? searchTerm,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var myRentals = await _rentalService.GetRentalsByOwnerAsync(user.Id);

                // Apply filters
                if (status.HasValue)
                {
                    myRentals = myRentals.Where(r => r.Status == status.Value);
                }

                if (startDate.HasValue)
                {
                    myRentals = myRentals.Where(r => r.StartDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    myRentals = myRentals.Where(r => r.EndDate.Date <= endDate.Value.Date);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    myRentals = myRentals.Where(r =>
                        (r.Car != null && r.Car.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (r.Renter != null && r.Renter.FullName != null &&
                         r.Renter.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = myRentals.Count();
                var pagedRentals = myRentals
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Pass filter values to ViewBag
                ViewBag.Status = status;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < ViewBag.TotalPages;

                // Helper functions
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);

                return View(pagedRentals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rentals for owner: {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thuê xe.";

                ViewBag.PageNumber = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = 0;
                ViewBag.TotalPages = 0;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);

                return View(new List<Rental>());
            }
        }

        // GET: CarOwner/Rentals/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _rentalService.GetRentalWithDetailsAsync(id);

                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê hoặc bạn không có quyền xem đơn thuê này.";
                    return RedirectToAction(nameof(Index));
                }

                var payments = await _paymentService.GetPaymentsByRentalAsync(id);
                var completedPayments = payments.Where(p => p.Status == PaymentStatus.Completed);
                var totalPaid = completedPayments.Sum(p => p.Amount);
                var remainingAmount = rental.TotalPrice - totalPaid;
                var hasPendingPayments = payments.Any(p => p.Status == PaymentStatus.Pending);

                var isFullyPaid = remainingAmount <= 0;
                var depositRequired = rental.TotalPrice * 0.3m;
                var depositPaid = Math.Min(totalPaid, depositRequired);
                var hasDeposit = Math.Round(depositPaid, 0) >= Math.Round(depositRequired, 0);

                // ✅ LOGIC MỚI: Xác nhận phụ thuộc vào driver tự nhận đơn
                bool canConfirm;
                
                if (rental.RequiresDriver)
                {
                    // Chỉ cho phép xác nhận khi driver đã tự nhận đơn (DriverAccepted == true)
                    canConfirm = rental.Status == RentalStatus.Pending && 
                                hasDeposit && 
                                !string.IsNullOrEmpty(rental.DriverId) && 
                                rental.DriverAccepted == true;
                }
                else
                {
                    // Đơn không cần tài xế → xác nhận bình thường
                    canConfirm = rental.Status == RentalStatus.Pending && hasDeposit;
                }

                var canStart = rental.Status == RentalStatus.Confirmed &&
                               DateTime.Now >= rental.StartDate.AddHours(-1) &&
                               hasDeposit;
                var canComplete = rental.Status == RentalStatus.Active && isFullyPaid;
                var canCancel = (rental.Status == RentalStatus.Pending ||
                                 rental.Status == RentalStatus.Confirmed) &&
                                DateTime.Now < rental.StartDate;

                ViewBag.Payments = payments.ToList();
                ViewBag.TotalPaid = totalPaid;
                ViewBag.RemainingAmount = remainingAmount;
                ViewBag.HasPendingPayments = hasPendingPayments;
                ViewBag.IsFullyPaid = isFullyPaid;
                ViewBag.DepositRequired = depositRequired;
                ViewBag.HasDeposit = hasDeposit;
                ViewBag.DepositPaid = Math.Min(totalPaid, depositRequired);
                ViewBag.CanConfirm = canConfirm;
                ViewBag.CanStart = canStart;
                ViewBag.CanComplete = canComplete;
                ViewBag.CanCancel = canCancel;

                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);
                ViewBag.GetCarStatusText = new Func<CarStatus, string>(GetCarStatusText);
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);
                ViewBag.GetPaymentStatusText = new Func<PaymentStatus, string>(GetPaymentStatusText);

                return View(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rental details: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn thuê.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CarOwner/Rentals/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _rentalService.GetRentalWithDetailsAsync(id);

                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê hoặc bạn không có quyền xác nhận.";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ NẾU ĐƠN CẦN TÀI XẾ: Kiểm tra driver đã tự nhận chưa
                if (rental.RequiresDriver)
                {
                    if (string.IsNullOrEmpty(rental.DriverId))
                    {
                        TempData["ErrorMessage"] = "Đơn thuê cần có tài xế. Vui lòng đợi tài xế tự nhận đơn.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    
                    if (rental.DriverAccepted != true)
                    {
                        TempData["ErrorMessage"] = "Tài xế chưa chấp nhận đơn thuê. Vui lòng đợi tài xế xác nhận.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                // ✅ Kiểm tra đặt cọc
                var payments = await _paymentService.GetPaymentsByRentalAsync(id);
                var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
                var depositRequired = rental.TotalPrice * 0.3m;

                if (Math.Round(totalPaid, 0) < Math.Round(depositRequired, 0))
                {
                    TempData["ErrorMessage"] = $"Không thể xác nhận đơn. Khách hàng cần thanh toán đặt cọc ít nhất {depositRequired:N0} VNĐ (30% tổng giá trị). Hiện tại đã thanh toán: {totalPaid:N0} VNĐ";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _rentalService.ConfirmRentalAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đã xác nhận đơn thuê xe thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xác nhận đơn thuê xe.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xác nhận đơn thuê.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: CarOwner/Rentals/Start/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _rentalService.GetRentalWithDetailsAsync(id);

                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê hoặc bạn không có quyền bắt đầu.";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ KIỂM TRA THANH TOÁN ĐẶT CỌC với làm tròn
                var payments = await _paymentService.GetPaymentsByRentalAsync(id);
                var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
                var depositRequired = rental.TotalPrice * 0.3m;

                // ✅ FIX: Làm tròn trước khi so sánh
                if (Math.Round(totalPaid, 0) < Math.Round(depositRequired, 0))
                {
                    TempData["ErrorMessage"] = $"Không thể bắt đầu đơn thuê. Khách hàng cần thanh toán đặt cọc đầy đủ ({depositRequired:N0} VNĐ). Hiện tại: {totalPaid:N0} VNĐ";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _rentalService.StartRentalAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đã bắt đầu đơn thuê xe thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể bắt đầu đơn thuê xe.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi bắt đầu đơn thuê.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: CarOwner/Rentals/Complete/5
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _rentalService.GetRentalWithDetailsAsync(id);

                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê hoặc bạn không có quyền hoàn thành.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Status != RentalStatus.Active)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hoàn thành hợp đồng đang hoạt động.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // ✅ KIỂM TRA THANH TOÁN ĐẦY ĐỦ
                var payments = await _paymentService.GetPaymentsByRentalAsync(id);
                var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
                var remainingAmount = rental.TotalPrice - totalPaid;

                if (remainingAmount > 0)
                {
                    TempData["ErrorMessage"] = $"Không thể hoàn thành đơn thuê. Khách hàng còn nợ {remainingAmount:N0} VNĐ. Vui lòng yêu cầu khách thanh toán trước khi hoàn thành.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Tính phí trễ nếu quá hạn
                decimal calculatedLateFee = 0;
                if (DateTime.Now > rental.EndDate)
                {
                    var lateDays = (DateTime.Now - rental.EndDate).Days;
                    calculatedLateFee = lateDays * (rental.Car?.PricePerDay ?? 0) * 0.1m;
                }

                ViewBag.CalculatedLateFee = calculatedLateFee;
                ViewBag.IsOverdue = DateTime.Now > rental.EndDate;
                ViewBag.LateDays = DateTime.Now > rental.EndDate ? (DateTime.Now - rental.EndDate).Days : 0;
                ViewBag.TotalPaid = totalPaid;
                ViewBag.RemainingAmount = remainingAmount;

                return View(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading complete rental form: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form hoàn thành hợp đồng.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: CarOwner/Rentals/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, decimal? damageFee, string? completionNotes)
        {
            Rental? rental = null; // Khai báo biến ở ngoài try block

            try
            {
                var user = await _userManager.GetUserAsync(User);
                rental = await _rentalService.GetRentalWithDetailsAsync(id);

                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thuê xe hoặc bạn không có quyền thao tác.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Status != RentalStatus.Active)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hoàn thành thuê xe ở trạng thái 'Đang thuê'.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _rentalService.CompleteRentalAsync(id, damageFee, completionNotes);

                TempData["SuccessMessage"] = "Đã hoàn thành thuê xe thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hoàn thành thuê xe.";

                // Kiểm tra null trước khi sử dụng
                if (rental != null)
                {
                    ViewBag.CalculatedLateFee = rental.EndDate < DateTime.Now
                        ? await _rentalService.CalculateLateFeeAsync(id)
                        : (decimal?)null;

                    return View(rental);
                }

                // Nếu rental là null, redirect về Index
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CarOwner/Rentals/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var rental = await _rentalService.GetRentalWithDetailsAsync(id);

                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thuê xe hoặc bạn không có quyền thao tác.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Status != RentalStatus.Pending && rental.Status != RentalStatus.Confirmed)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hủy thuê xe ở trạng thái 'Chờ xác nhận' hoặc 'Đã xác nhận'.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _rentalService.CancelRentalAsync(id, reason ?? "Hủy bởi chủ xe");
                TempData["SuccessMessage"] = "Đã hủy thuê xe thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy thuê xe.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        #region Helper Methods

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

        private string GetPaymentMethodText(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Tiền mặt",
                PaymentMethod.BankTransfer => "Chuyển khoản",
                PaymentMethod.CreditCard => "Thẻ tín dụng",
                PaymentMethod.EWallet => "Ví điện tử",
                _ => "Không xác định"
            };
        }

        private string GetPaymentTypeText(PaymentType type)
        {
            return type switch
            {
                PaymentType.Deposit => "Đặt cọc",
                PaymentType.RentalFee => "Tiền thuê",
                PaymentType.LateFee => "Phí trễ",
                PaymentType.DamageFee => "Phí hư hỏng",
                PaymentType.Refund => "Hoàn tiền",
                PaymentType.FullPayment => "Thanh toán đầy đủ",
                _ => "Không xác định"
            };
        }

        private string GetPaymentStatusText(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Pending => "Chờ thanh toán",
                PaymentStatus.Completed => "Đã thanh toán",
                PaymentStatus.Failed => "Thất bại",
                PaymentStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        #endregion
    }
}