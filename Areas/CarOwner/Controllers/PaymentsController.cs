using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.CarOwner.Controllers
{
    [Area("CarOwner")]
    [Authorize(Roles = RoleConstants.Owner)]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IRentalService _rentalService;
        private readonly IPaymentDistributionService _distributionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            IRentalService rentalService,
            IPaymentDistributionService distributionService,
            UserManager<ApplicationUser> userManager,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _rentalService = rentalService;
            _distributionService = distributionService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: CarOwner/Payments
        public async Task<IActionResult> Index(PaymentStatus? status, int page = 1, int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Get all rentals of cars owned by current user
                var myRentals = await _rentalService.GetRentalsByOwnerAsync(user.Id);
                var allPayments = new List<Payment>();

                foreach (var rental in myRentals)
                {
                    var payments = await _paymentService.GetPaymentsByRentalAsync(rental.RentalId);
                    allPayments.AddRange(payments);
                }

                // Apply status filter
                if (status.HasValue)
                {
                    allPayments = allPayments.Where(p => p.Status == status.Value).ToList();
                }

                var totalCount = allPayments.Count;
                var pagedPayments = allPayments
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Pass filter and pagination to ViewBag
                ViewBag.Status = status;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < ViewBag.TotalPages;

                // Helper functions
                ViewBag.GetPaymentStatusText = new Func<PaymentStatus, string>(GetPaymentStatusText);
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);

                return View(pagedPayments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payments for car owner: {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thanh toán.";

                ViewBag.PageNumber = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = 0;
                ViewBag.TotalPages = 0;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;
                ViewBag.GetPaymentStatusText = new Func<PaymentStatus, string>(GetPaymentStatusText);
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);

                return View(new List<Payment>());
            }
        }

        // GET: CarOwner/Payments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var payment = await _paymentService.GetPaymentByIdAsync(id);

                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thanh toán.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if this payment belongs to owner's rental
                var rental = await _rentalService.GetRentalWithDetailsAsync(payment.RentalId);
                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xem thanh toán này.";
                    return RedirectToAction(nameof(Index));
                }

                // Pass helper functions to ViewBag
                ViewBag.GetPaymentStatusText = new Func<PaymentStatus, string>(GetPaymentStatusText);
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);

                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment details: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin thanh toán.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CarOwner/Payments/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id, string? transactionId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var payment = await _paymentService.GetPaymentByIdAsync(id);

                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thanh toán.";
                    return RedirectToAction(nameof(Index));
                }

                var rental = await _rentalService.GetRentalByIdAsync(payment.RentalId);
                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xác nhận thanh toán này.";
                    return RedirectToAction(nameof(Index));
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể xác nhận thanh toán đang chờ xử lý.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Generate transaction ID if not provided
                if (string.IsNullOrEmpty(transactionId))
                {
                    transactionId = $"TXN_{payment.PaymentId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                }

                var result = await _paymentService.ProcessPaymentAsync(id, transactionId);

                if (result)
                {
                    // Create payment distribution automatically
                    try
                    {
                        var distributionCreated = await _distributionService.CreateDistributionAsync(id);
                        if (distributionCreated)
                        {
                            _logger.LogInformation("Payment distribution created successfully for payment: {PaymentId}", id);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create payment distribution for payment: {PaymentId}", id);
                        }
                    }
                    catch (Exception distEx)
                    {
                        _logger.LogError(distEx, "Error creating payment distribution for payment: {PaymentId}", id);
                        // Don't fail the whole operation if distribution fails
                    }

                    TempData["SuccessMessage"] = $"Đã xác nhận thanh toán thành công. Số tiền: {payment.Amount:N0} VNĐ";
                    return RedirectToAction("Details", "Rentals", new { id = payment.RentalId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xác nhận thanh toán.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xác nhận thanh toán.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: CarOwner/Payments/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var payment = await _paymentService.GetPaymentByIdAsync(id);

                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thanh toán.";
                    return RedirectToAction(nameof(Index));
                }

                var rental = await _rentalService.GetRentalByIdAsync(payment.RentalId);
                if (rental == null || rental.Car?.OwnerId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền từ chối thanh toán này.";
                    return RedirectToAction(nameof(Index));
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể từ chối thanh toán đang chờ xử lý.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _paymentService.MarkPaymentFailedAsync(id);

                if (result)
                {
                    TempData["SuccessMessage"] = "Đã từ chối thanh toán.";
                    if (!string.IsNullOrEmpty(reason))
                    {
                        // TODO: Send notification to customer with reason
                    }
                    return RedirectToAction("Details", "Rentals", new { id = payment.RentalId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể từ chối thanh toán.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payment: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi từ chối thanh toán.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #region Private Helper Methods

        private string GetPaymentStatusText(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Pending => "Chờ xác nhận",
                PaymentStatus.Completed => "Đã thanh toán",
                PaymentStatus.Failed => "Thất bại",
                PaymentStatus.Cancelled => "Đã hủy",
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
                _ => "Khác"
            };
        }

        private string GetPaymentTypeText(PaymentType type)
        {
            return type switch
            {
                PaymentType.Deposit => "Đặt cọc",
                PaymentType.Rental => "Tiền thuê",
                PaymentType.LateFee => "Phí trễ hạn",
                PaymentType.DamageFee => "Phí hư hỏng",
                PaymentType.Refund => "Hoàn tiền",
                PaymentType.FullPayment => "Thanh toán đầy đủ",
                PaymentType.RentalFee => "Tiền thuê xe",
                _ => "Khác"
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

        #endregion
    }
}
