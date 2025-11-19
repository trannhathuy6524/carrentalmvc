using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IRentalService _rentalService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            IRentalService rentalService,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _rentalService = rentalService;
            _logger = logger;
        }

        // GET: Admin/Payments
        public async Task<IActionResult> Index(
            PaymentStatus? status,
            PaymentMethod? paymentMethod,
            PaymentType? paymentType,
            DateTime? startDate,
            DateTime? endDate,
            string? searchTerm,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var allPayments = await _paymentService.GetAllPaymentsAsync();

                // Calculate summary statistics first
                var totalRevenue = allPayments
                    .Where(p => p.Status == PaymentStatus.Completed && p.Amount > 0)
                    .Sum(p => p.Amount);

                var pendingPayments = allPayments.Count(p => p.Status == PaymentStatus.Pending);
                var completedPayments = allPayments.Count(p => p.Status == PaymentStatus.Completed);
                var failedPayments = allPayments.Count(p => p.Status == PaymentStatus.Failed);
                var cancelledPayments = allPayments.Count(p => p.Status == PaymentStatus.Cancelled);

                // Apply filters
                var filteredPayments = allPayments.AsQueryable();

                if (status.HasValue)
                {
                    filteredPayments = filteredPayments.Where(p => p.Status == status.Value);
                }

                if (paymentMethod.HasValue)
                {
                    filteredPayments = filteredPayments.Where(p => p.PaymentMethod == paymentMethod.Value);
                }

                if (paymentType.HasValue)
                {
                    filteredPayments = filteredPayments.Where(p => p.PaymentType == paymentType.Value);
                }

                if (startDate.HasValue)
                {
                    filteredPayments = filteredPayments.Where(p =>
                        p.PaymentDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    filteredPayments = filteredPayments.Where(p =>
                        p.PaymentDate.Date <= endDate.Value.Date);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    filteredPayments = filteredPayments.Where(p =>
                        (p.TransactionId != null && p.TransactionId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        p.PaymentId.ToString().Contains(searchTerm) ||
                        p.RentalId.ToString().Contains(searchTerm) ||
                        (p.Rental != null && p.Rental.Renter != null && p.Rental.Renter.FullName != null &&
                         p.Rental.Renter.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = filteredPayments.Count();
                var pagedPayments = filteredPayments
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var filteredTotalAmount = filteredPayments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);

                // Sử dụng ViewBag để truyền dữ liệu filter và phân trang
                ViewBag.Status = status;
                ViewBag.PaymentMethod = paymentMethod;
                ViewBag.PaymentType = paymentType;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Statistics
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalAmount = filteredTotalAmount;
                ViewBag.PendingPayments = pendingPayments;
                ViewBag.CompletedPayments = completedPayments;
                ViewBag.FailedPayments = failedPayments;
                ViewBag.CancelledPayments = cancelledPayments;

                // Helper functions cho view
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);
                ViewBag.GetPaymentStatusText = new Func<PaymentStatus, string>(GetPaymentStatusText);

                // Additional statistics
                ViewBag.TotalPayments = allPayments.Count();
                ViewBag.DepositPayments = allPayments.Count(p => p.PaymentType == PaymentType.Deposit);
                ViewBag.RentalFeePayments = allPayments.Count(p => p.PaymentType == PaymentType.RentalFee);
                ViewBag.RefundPayments = allPayments.Count(p => p.PaymentType == PaymentType.Refund);

                // Payment method statistics
                ViewBag.CashPayments = allPayments.Count(p => p.PaymentMethod == PaymentMethod.Cash);
                ViewBag.BankTransferPayments = allPayments.Count(p => p.PaymentMethod == PaymentMethod.BankTransfer);
                ViewBag.CreditCardPayments = allPayments.Count(p => p.PaymentMethod == PaymentMethod.CreditCard);
                ViewBag.EWalletPayments = allPayments.Count(p => p.PaymentMethod == PaymentMethod.EWallet);

                return View(pagedPayments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payments list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thanh toán.";
                return View(new List<Payment>());
            }
        }

        // GET: Admin/Payments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán.";
                    return RedirectToAction(nameof(Index));
                }

                var rental = await _rentalService.GetRentalWithDetailsAsync(payment.RentalId);
                var relatedPayments = await _paymentService.GetPaymentsByRentalAsync(payment.RentalId);

                // Truyền thông tin bổ sung qua ViewBag
                ViewBag.PaymentMethodText = GetPaymentMethodText(payment.PaymentMethod);
                ViewBag.PaymentTypeText = GetPaymentTypeText(payment.PaymentType);
                ViewBag.StatusText = GetPaymentStatusText(payment.Status);
                ViewBag.CarName = rental?.Car?.Name ?? "N/A";
                ViewBag.RenterName = rental?.Renter?.FullName ?? "N/A";
                ViewBag.RenterEmail = rental?.Renter?.Email ?? "N/A";
                ViewBag.RenterPhone = rental?.Renter?.PhoneNumber ?? "N/A";

                // Related payments (exclude current payment)
                ViewBag.RelatedPayments = relatedPayments.Where(p => p.PaymentId != payment.PaymentId).ToList();

                // Action permissions
                ViewBag.CanMarkCompleted = payment.Status == PaymentStatus.Pending;
                ViewBag.CanMarkFailed = payment.Status == PaymentStatus.Pending;
                ViewBag.CanRefund = payment.Status == PaymentStatus.Completed && payment.Amount > 0;

                // Helper functions
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);
                ViewBag.GetPaymentStatusText = new Func<PaymentStatus, string>(GetPaymentStatusText);

                // Rental information
                ViewBag.Rental = rental;

                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment details for ID: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin thanh toán.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Payments/MarkCompleted/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int id)
        {
            try
            {
                var result = await _paymentService.MarkPaymentCompletedAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Thanh toán đã được đánh dấu hoàn thành.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật trạng thái thanh toán.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking payment as completed: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thanh toán.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Payments/MarkFailed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFailed(int id)
        {
            try
            {
                var result = await _paymentService.MarkPaymentFailedAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Thanh toán đã được đánh dấu thất bại.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật trạng thái thanh toán.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking payment as failed: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thanh toán.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Payments/Refund/5
        public async Task<IActionResult> Refund(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán.";
                    return RedirectToAction(nameof(Index));
                }

                if (payment.Status != PaymentStatus.Completed || payment.Amount <= 0)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hoàn tiền cho thanh toán đã hoàn thành.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var rental = await _rentalService.GetRentalWithDetailsAsync(payment.RentalId);

                // Truyền thông tin qua ViewBag cho form refund
                ViewBag.OriginalAmount = payment.Amount;
                ViewBag.PaymentMethodText = GetPaymentMethodText(payment.PaymentMethod);
                ViewBag.CarName = rental?.Car?.Name ?? "N/A";
                ViewBag.RenterName = rental?.Renter?.FullName ?? "N/A";

                // Create a simple model for refund with just the necessary fields
                var refundModel = new Payment
                {
                    PaymentId = payment.PaymentId,
                    Amount = payment.Amount, // Default refund amount to original amount
                    PaymentDate = payment.PaymentDate
                };

                return View(refundModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading refund form for payment: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form hoàn tiền.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Payments/Refund/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id, decimal refundAmount, string? reason)
        {
            try
            {
                // Validate refund amount
                var originalPayment = await _paymentService.GetPaymentByIdAsync(id);
                if (originalPayment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán.";
                    return RedirectToAction(nameof(Index));
                }

                if (refundAmount <= 0 || refundAmount > originalPayment.Amount)
                {
                    TempData["ErrorMessage"] = "Số tiền hoàn không hợp lệ.";
                    return RedirectToAction(nameof(Refund), new { id });
                }

                var result = await _paymentService.RefundPaymentAsync(id, refundAmount, reason);

                if (result)
                {
                    TempData["SuccessMessage"] = "Hoàn tiền thành công.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hoàn tiền.";
                    return RedirectToAction(nameof(Refund), new { id });
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Refund), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hoàn tiền.";
                return RedirectToAction(nameof(Refund), new { id });
            }
        }

        // Helper methods
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
    }
}