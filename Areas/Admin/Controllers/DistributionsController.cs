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
    public class DistributionsController : Controller
    {
        private readonly IPaymentDistributionService _distributionService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<DistributionsController> _logger;

        public DistributionsController(
            IPaymentDistributionService distributionService,
            IPaymentService paymentService,
            ILogger<DistributionsController> logger)
        {
            _distributionService = distributionService;
            _paymentService = paymentService;
            _logger = logger;
        }

        // GET: Admin/Distributions
        public async Task<IActionResult> Index(
            PaymentDistributionStatus? status,
            RecipientType? recipientType,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                // Get all distributions
                var allDistributions = new List<PaymentDistribution>();

                if (status.HasValue)
                {
                    var distributions = await _distributionService.GetPendingDistributionsAsync();
                    allDistributions = distributions.ToList();
                }
                else
                {
                    // Get all - this needs to be added to service
                    var pending = await _distributionService.GetPendingDistributionsAsync();
                    allDistributions = pending.ToList();
                }

                // Apply filters
                if (recipientType.HasValue)
                {
                    allDistributions = allDistributions
                        .Where(d => d.RecipientType == recipientType.Value)
                        .ToList();
                }

                if (startDate.HasValue)
                {
                    allDistributions = allDistributions
                        .Where(d => d.CreatedAt >= startDate.Value)
                        .ToList();
                }

                if (endDate.HasValue)
                {
                    allDistributions = allDistributions
                        .Where(d => d.CreatedAt < endDate.Value.AddDays(1))
                        .ToList();
                }

                // Calculate statistics
                var totalAmount = allDistributions.Sum(d => d.Amount);
                var platformAmount = allDistributions
                    .Where(d => d.RecipientType == RecipientType.Platform)
                    .Sum(d => d.Amount);
                var ownerAmount = allDistributions
                    .Where(d => d.RecipientType == RecipientType.CarOwner)
                    .Sum(d => d.Amount);
                var driverAmount = allDistributions
                    .Where(d => d.RecipientType == RecipientType.Driver)
                    .Sum(d => d.Amount);

                ViewBag.TotalAmount = totalAmount;
                ViewBag.PlatformAmount = platformAmount;
                ViewBag.OwnerAmount = ownerAmount;
                ViewBag.DriverAmount = driverAmount;

                // Pagination
                var totalCount = allDistributions.Count;
                var pagedDistributions = allDistributions
                    .OrderByDescending(d => d.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Pass filters to view
                ViewBag.Status = status;
                ViewBag.RecipientType = recipientType;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < ViewBag.TotalPages;

                // Helper functions
                ViewBag.GetStatusText = new Func<PaymentDistributionStatus, string>(GetStatusText);
                ViewBag.GetRecipientTypeText = new Func<RecipientType, string>(GetRecipientTypeText);

                return View(pagedDistributions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading distributions");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách phân phối.";
                return View(new List<PaymentDistribution>());
            }
        }

        // GET: Admin/Distributions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var distributions = await _distributionService.GetDistributionsByPaymentAsync(id);
                var distribution = distributions.FirstOrDefault(d => d.PaymentDistributionId == id);

                if (distribution == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phân phối.";
                    return RedirectToAction(nameof(Index));
                }

                // Get payment details
                var payment = await _paymentService.GetPaymentByIdAsync(distribution.PaymentId);
                ViewBag.Payment = payment;

                // Get all distributions for this payment
                var allDistributions = await _distributionService.GetDistributionsByPaymentAsync(distribution.PaymentId);
                ViewBag.AllDistributions = allDistributions;

                // Helper functions
                ViewBag.GetStatusText = new Func<PaymentDistributionStatus, string>(GetStatusText);
                ViewBag.GetRecipientTypeText = new Func<RecipientType, string>(GetRecipientTypeText);

                return View(distribution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading distribution details: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin phân phối.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Distributions/MarkCompleted/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int id, string transactionReference)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionReference))
                {
                    transactionReference = $"ADM_TXN_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                }

                var result = await _distributionService.MarkDistributionCompletedAsync(id, transactionReference);

                if (result)
                {
                    TempData["SuccessMessage"] = "Đã đánh dấu phân phối hoàn thành.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể đánh dấu phân phối hoàn thành.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking distribution completed: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Admin/Distributions/MarkFailed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFailed(int id, string errorMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = "Marked as failed by admin";
                }

                var result = await _distributionService.MarkDistributionFailedAsync(id, errorMessage);

                if (result)
                {
                    TempData["SuccessMessage"] = "Đã đánh dấu phân phối thất bại.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể đánh dấu phân phối thất bại.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking distribution failed: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Admin/Distributions/Statistics
        public async Task<IActionResult> Statistics(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                // Get all pending distributions for statistics
                var allDistributions = await _distributionService.GetPendingDistributionsAsync();

                // Calculate stats
                var stats = new
                {
                    TotalDistributions = allDistributions.Count(),
                    PendingDistributions = allDistributions.Count(d => d.Status == PaymentDistributionStatus.Pending),
                    CompletedDistributions = allDistributions.Count(d => d.Status == PaymentDistributionStatus.Completed),
                    FailedDistributions = allDistributions.Count(d => d.Status == PaymentDistributionStatus.Failed),

                    TotalAmount = allDistributions.Sum(d => d.Amount),
                    PendingAmount = allDistributions.Where(d => d.Status == PaymentDistributionStatus.Pending).Sum(d => d.Amount),
                    CompletedAmount = allDistributions.Where(d => d.Status == PaymentDistributionStatus.Completed).Sum(d => d.Amount),

                    PlatformRevenue = allDistributions.Where(d => d.RecipientType == RecipientType.Platform).Sum(d => d.Amount),
                    OwnerRevenue = allDistributions.Where(d => d.RecipientType == RecipientType.CarOwner).Sum(d => d.Amount),
                    DriverRevenue = allDistributions.Where(d => d.RecipientType == RecipientType.Driver).Sum(d => d.Amount)
                };

                ViewBag.Stats = stats;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading statistics");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thống kê.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Helper Methods

        private string GetStatusText(PaymentDistributionStatus status)
        {
            return status switch
            {
                PaymentDistributionStatus.Pending => "Chờ xử lý",
                PaymentDistributionStatus.Processing => "Đang xử lý",
                PaymentDistributionStatus.Completed => "Đã hoàn thành",
                PaymentDistributionStatus.Failed => "Thất bại",
                PaymentDistributionStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        private string GetRecipientTypeText(RecipientType type)
        {
            return type switch
            {
                RecipientType.Platform => "Nền tảng",
                RecipientType.CarOwner => "Chủ xe",
                RecipientType.Driver => "Tài xế",
                _ => "Khác"
            };
        }

        #endregion
    }
}
