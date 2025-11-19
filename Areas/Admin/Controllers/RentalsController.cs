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
    public class RentalsController : Controller
    {
        private readonly IRentalService _rentalService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<RentalsController> _logger;

        public RentalsController(
            IRentalService rentalService,
            IPaymentService paymentService,
            ILogger<RentalsController> logger)
        {
            _rentalService = rentalService;
            _paymentService = paymentService;
            _logger = logger;
        }

        // GET: Admin/Rentals
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
                var allRentals = await _rentalService.GetAllRentalsAsync();

                // Apply filters
                var filteredRentals = allRentals.AsQueryable();

                if (status.HasValue)
                {
                    filteredRentals = filteredRentals.Where(r => r.Status == status.Value);
                }

                if (startDate.HasValue)
                {
                    filteredRentals = filteredRentals.Where(r => r.StartDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    filteredRentals = filteredRentals.Where(r => r.EndDate <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    filteredRentals = filteredRentals.Where(r =>
                        (r.Car != null && r.Car.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (r.Renter != null && r.Renter.FullName != null && r.Renter.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        r.RentalId.ToString().Contains(searchTerm));
                }

                var totalCount = filteredRentals.Count();
                var pagedRentals = filteredRentals
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Sử dụng ViewBag để truyền dữ liệu filter và phân trang
                ViewBag.Status = status;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Helper functions cho view
                ViewBag.GetRentalStatusText = new Func<RentalStatus, string>(GetRentalStatusText);
                ViewBag.GetTotalDays = new Func<DateTime, DateTime, int>((start, end) => (end - start).Days);

                // Thống kê tổng quan
                ViewBag.TotalRentals = allRentals.Count();
                ViewBag.PendingRentals = allRentals.Count(r => r.Status == RentalStatus.Pending);
                ViewBag.ConfirmedRentals = allRentals.Count(r => r.Status == RentalStatus.Confirmed);
                ViewBag.ActiveRentals = allRentals.Count(r => r.Status == RentalStatus.Active);
                ViewBag.CompletedRentals = allRentals.Count(r => r.Status == RentalStatus.Completed);
                ViewBag.CancelledRentals = allRentals.Count(r => r.Status == RentalStatus.Cancelled);
                ViewBag.OverdueRentals = allRentals.Count(r => r.Status == RentalStatus.Overdue);

                // Thống kê tài chính
                ViewBag.TotalRevenue = allRentals.Where(r => r.Status == RentalStatus.Completed).Sum(r => r.TotalPrice);
                ViewBag.PendingRevenue = allRentals.Where(r => r.Status == RentalStatus.Active || r.Status == RentalStatus.Confirmed).Sum(r => r.TotalPrice);

                return View(pagedRentals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rentals list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thuê xe.";
                return View(new List<Rental>());
            }
        }

        // GET: Admin/Rentals/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var rental = await _rentalService.GetRentalWithDetailsAsync(id);
                if (rental == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hợp đồng thuê xe.";
                    return RedirectToAction(nameof(Index));
                }

                var payments = await _paymentService.GetPaymentsByRentalAsync(id);

                // Tính toán các thông tin bổ sung
                var totalPaid = payments.Where(p => p.Status == PaymentStatus.Completed && p.Amount > 0).Sum(p => p.Amount);
                var remainingAmount = rental.TotalPrice - totalPaid;
                var hasPendingPayments = payments.Any(p => p.Status == PaymentStatus.Pending);

                // Truyền thông tin qua ViewBag
                ViewBag.StatusText = GetRentalStatusText(rental.Status);
                ViewBag.TotalPaid = totalPaid;
                ViewBag.RemainingAmount = remainingAmount;
                ViewBag.HasPendingPayments = hasPendingPayments;
                ViewBag.TotalDays = (rental.EndDate - rental.StartDate).Days;

                // Action permissions
                ViewBag.CanConfirm = rental.Status == RentalStatus.Pending;
                ViewBag.CanStart = rental.Status == RentalStatus.Confirmed;
                ViewBag.CanComplete = rental.Status == RentalStatus.Active;
                ViewBag.CanCancel = rental.Status == RentalStatus.Pending || rental.Status == RentalStatus.Confirmed;

                // Helper functions
                ViewBag.GetPaymentMethodText = new Func<PaymentMethod, string>(GetPaymentMethodText);
                ViewBag.GetPaymentTypeText = new Func<PaymentType, string>(GetPaymentTypeText);
                ViewBag.GetPaymentStatusText = new Func<PaymentStatus, string>(GetPaymentStatusText);

                // Car image
                ViewBag.CarImageUrl = rental.Car?.CarImages?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl;

                // Truyền payments list
                ViewBag.Payments = payments.ToList();

                return View(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading rental details for ID: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin hợp đồng thuê xe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Rentals/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            try
            {
                var result = await _rentalService.ConfirmRentalAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Hợp đồng thuê xe đã được xác nhận thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xác nhận hợp đồng thuê xe.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xác nhận hợp đồng thuê xe.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Rentals/Start/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            try
            {
                var result = await _rentalService.StartRentalAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Hợp đồng thuê xe đã bắt đầu thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể bắt đầu hợp đồng thuê xe.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi bắt đầu hợp đồng thuê xe.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Rentals/Complete/5
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var rental = await _rentalService.GetRentalWithDetailsAsync(id);
                if (rental == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hợp đồng thuê xe.";
                    return RedirectToAction(nameof(Index));
                }

                if (rental.Status != RentalStatus.Active)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hoàn thành hợp đồng đang hoạt động.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Tính phí trễ nếu quá hạn
                decimal calculatedLateFee = 0;
                if (DateTime.Now > rental.EndDate)
                {
                    var lateDays = (DateTime.Now - rental.EndDate).Days;
                    calculatedLateFee = lateDays * (rental.Car?.PricePerDay ?? 0) * 0.1m; // 10% per day
                }

                // Truyền thông tin qua ViewBag
                ViewBag.CalculatedLateFee = calculatedLateFee;
                ViewBag.IsOverdue = DateTime.Now > rental.EndDate;
                ViewBag.LateDays = DateTime.Now > rental.EndDate ? (DateTime.Now - rental.EndDate).Days : 0;

                return View(rental);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading complete rental form: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form hoàn thành hợp đồng.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Rentals/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, decimal? damageFee, string? completionNotes)
        {
            try
            {
                var result = await _rentalService.CompleteRentalAsync(
                    id,
                    damageFee,
                    completionNotes);

                if (result)
                {
                    TempData["SuccessMessage"] = "Hợp đồng thuê xe đã được hoàn thành thành công.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hoàn thành hợp đồng thuê xe.";
                    return RedirectToAction(nameof(Complete), new { id });
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Complete), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hoàn thành hợp đồng thuê xe.";
                return RedirectToAction(nameof(Complete), new { id });
            }
        }

        // POST: Admin/Rentals/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason = "")
        {
            try
            {
                var result = await _rentalService.CancelRentalAsync(id, reason);
                if (result)
                {
                    TempData["SuccessMessage"] = "Hợp đồng thuê xe đã được hủy thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy hợp đồng thuê xe.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling rental: {RentalId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy hợp đồng thuê xe.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Helper methods
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