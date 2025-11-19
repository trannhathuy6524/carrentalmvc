using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = RoleConstants.Customer)]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IRentalService _rentalService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            IRentalService rentalService,
            UserManager<ApplicationUser> userManager,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _rentalService = rentalService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Customer/Payments
        public async Task<IActionResult> Index(PaymentStatus? status, int page = 1, int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Get all rentals of the user first
                var myRentals = await _rentalService.GetRentalsByUserAsync(user.Id);
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
                _logger.LogError(ex, "Error loading payments for customer: {UserId}", User.Identity?.Name);
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

        // GET: Customer/Payments/Details/5
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

                // Check if this payment belongs to user's rental
                var rental = await _rentalService.GetRentalWithDetailsAsync(payment.RentalId);
                if (rental == null || rental.RenterId != user?.Id)
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

        // GET: Customer/Payments/Create
        public async Task<IActionResult> Create(int rentalId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var rental = await _rentalService.GetRentalWithDetailsAsync(rentalId);

                if (rental == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê.";
                    return RedirectToAction("Index", "Rentals");
                }

                if (rental.RenterId != user.Id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền thanh toán cho đơn thuê này.";
                    return RedirectToAction("Index", "Rentals");
                }

                if (rental.Status == RentalStatus.Cancelled || rental.Status == RentalStatus.Completed)
                {
                    TempData["ErrorMessage"] = "Không thể thanh toán cho đơn thuê đã hủy hoặc đã hoàn thành.";
                    return RedirectToAction("Details", "Rentals", new { id = rentalId });
                }

                // ✅ Calculate payment information
                var payments = await _paymentService.GetPaymentsByRentalAsync(rentalId);
                var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
                var remainingAmount = rental.TotalPrice - totalPaid;

                if (remainingAmount <= 0)
                {
                    TempData["ErrorMessage"] = "Đơn thuê này đã được thanh toán đầy đủ.";
                    return RedirectToAction("Details", "Rentals", new { id = rentalId });
                }

                // ✅ Calculate deposit information
                var depositRequired = rental.TotalPrice * 0.3m; // 30%
                var depositPaid = Math.Min(totalPaid, depositRequired);
                var depositRemaining = Math.Max(0, depositRequired - depositPaid);
                var hasDeposit = depositPaid >= depositRequired;

                // ✅ Determine suggested payment type and amount
                PaymentType suggestedPaymentType;
                decimal suggestedAmount;

                if (!hasDeposit && depositRemaining > 0)
                {
                    // Chưa đủ đặt cọc -> gợi ý thanh toán đặt cọc
                    suggestedPaymentType = PaymentType.Deposit;
                    suggestedAmount = depositRemaining;
                }
                else
                {
                    // Đã đủ đặt cọc -> gợi ý thanh toán phần còn lại
                    suggestedPaymentType = PaymentType.RentalFee;
                    suggestedAmount = remainingAmount;
                }

                // Pass data to ViewBag
                ViewBag.TotalPrice = rental.TotalPrice;
                ViewBag.TotalPaid = totalPaid;
                ViewBag.RemainingAmount = remainingAmount;
                ViewBag.DepositRequired = depositRequired;
                ViewBag.DepositPaid = depositPaid;
                ViewBag.DepositRemaining = depositRemaining;
                ViewBag.HasDeposit = hasDeposit;
                ViewBag.SuggestedPaymentType = suggestedPaymentType;
                ViewBag.SuggestedAmount = suggestedAmount;
                ViewBag.RentalStatus = rental.Status;

                // Helper functions
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);

                return View(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create payment form for rental: {RentalId}", rentalId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang thanh toán.";
                return RedirectToAction("Index", "Rentals");
            }
        }

        // POST: Customer/Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int rentalId,
            decimal amount,
            PaymentMethod paymentMethod,
            PaymentType paymentType,
            string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var rental = await _rentalService.GetRentalWithDetailsAsync(rentalId);

                if (rental == null || rental.RenterId != user.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn thuê hoặc bạn không có quyền thanh toán.";
                    return RedirectToAction("Index", "Rentals");
                }

                // Validate amount
                if (amount <= 0)
                {
                    TempData["ErrorMessage"] = "Số tiền thanh toán phải lớn hơn 0.";
                    return RedirectToAction(nameof(Create), new { rentalId });
                }

                var payments = await _paymentService.GetPaymentsByRentalAsync(rentalId);
                var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
                var remainingAmount = rental.TotalPrice - totalPaid;

                if (amount > remainingAmount)
                {
                    TempData["ErrorMessage"] = $"Số tiền thanh toán không được vượt quá số tiền còn lại ({remainingAmount:N0} VNĐ).";
                    return RedirectToAction(nameof(Create), new { rentalId });
                }

                // ✅ Validate deposit payment
                var depositRequired = rental.TotalPrice * 0.3m;
                var depositPaid = Math.Min(totalPaid, depositRequired);

                if (paymentType == PaymentType.Deposit)
                {
                    var depositRemaining = depositRequired - depositPaid;
                    if (amount > depositRemaining)
                    {
                        TempData["ErrorMessage"] = $"Số tiền đặt cọc không được vượt quá số tiền đặt cọc còn thiếu ({depositRemaining:N0} VNĐ).";
                        return RedirectToAction(nameof(Create), new { rentalId });
                    }
                }

                // Create payment
                var payment = new Payment
                {
                    RentalId = rentalId,
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    PaymentType = paymentType,
                    Status = PaymentStatus.Pending,
                    Notes = notes,
                    PaymentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdPayment = await _paymentService.CreatePaymentAsync(payment);

                // TODO: Integrate with actual payment gateway
                // For now, we'll mark as completed immediately for cash/bank transfer
                if (paymentMethod == PaymentMethod.Cash || paymentMethod == PaymentMethod.BankTransfer)
                {
                    var paymentTypeText = GetPaymentTypeText(paymentType);
                    TempData["SuccessMessage"] = $"Yêu cầu {paymentTypeText.ToLower()} đã được tạo ({amount:N0} VNĐ). Vui lòng chờ xác nhận từ chủ xe.";
                }
                else
                {
                    // For credit card/e-wallet, redirect to payment gateway
                    TempData["InfoMessage"] = "Đang chuyển hướng đến cổng thanh toán...";
                    // TODO: Redirect to payment gateway
                }

                return RedirectToAction("Details", "Rentals", new { id = rentalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for rental: {RentalId}", rentalId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo thanh toán.";
                return RedirectToAction(nameof(Create), new { rentalId });
            }
        }

        // GET: Customer/Payments/Cancel/5
        public async Task<IActionResult> Cancel(int id)
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
                if (rental == null || rental.RenterId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền hủy thanh toán này.";
                    return RedirectToAction(nameof(Index));
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hủy thanh toán đang chờ xử lý.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Helper functions
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);

                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cancel payment page: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Payments/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id, string? reason)
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
                if (rental == null || rental.RenterId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền hủy thanh toán này.";
                    return RedirectToAction(nameof(Index));
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hủy thanh toán đang chờ xử lý.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _paymentService.CancelPaymentAsync(id, reason ?? "Khách hàng yêu cầu hủy");

                if (result)
                {
                    TempData["SuccessMessage"] = "Đã hủy thanh toán thành công.";
                    return RedirectToAction("Details", "Rentals", new { id = payment.RentalId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy thanh toán này.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment: {PaymentId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy thanh toán.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #region Private Helper Methods

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