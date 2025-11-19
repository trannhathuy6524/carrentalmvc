using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carrentalmvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly ICarService _carService;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(
            IReviewService reviewService,
            ICarService carService,
            ILogger<ReviewsController> logger)
        {
            _reviewService = reviewService;
            _carService = carService;
            _logger = logger;
        }

        // GET: Admin/Reviews
        public async Task<IActionResult> Index(
            int? carId,
            int? rating,
            bool? isActive,
            bool? isRecommended,
            DateTime? startDate,
            DateTime? endDate,
            string? searchTerm,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var allReviews = await _reviewService.GetAllReviewsAsync();

                // Apply filters
                var filteredReviews = allReviews.AsQueryable();

                if (carId.HasValue)
                {
                    filteredReviews = filteredReviews.Where(r => r.CarId == carId.Value);
                }

                if (rating.HasValue)
                {
                    filteredReviews = filteredReviews.Where(r => r.Rating == rating.Value);
                }

                if (isActive.HasValue)
                {
                    filteredReviews = filteredReviews.Where(r => r.IsActive == isActive.Value);
                }

                if (isRecommended.HasValue)
                {
                    filteredReviews = filteredReviews.Where(r => r.IsRecommended == isRecommended.Value);
                }

                if (startDate.HasValue)
                {
                    filteredReviews = filteredReviews.Where(r => r.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    filteredReviews = filteredReviews.Where(r => r.CreatedAt <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    filteredReviews = filteredReviews.Where(r =>
                        (r.Car != null && r.Car.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (r.User != null && r.User.FullName != null && r.User.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (r.Comment != null && r.Comment.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = filteredReviews.Count();
                var pagedReviews = filteredReviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var averageRating = filteredReviews.Any() ? filteredReviews.Average(r => r.Rating) : 0;

                // Sử dụng ViewBag để truyền dữ liệu thay vì ViewModel
                ViewBag.CarId = carId;
                ViewBag.Rating = rating;
                ViewBag.IsActive = isActive;
                ViewBag.IsRecommended = isRecommended;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.AverageRating = averageRating;
                ViewBag.TotalReviewsCount = totalCount;

                return View(pagedReviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đánh giá.";
                return View(new List<Review>());
            }
        }

        // GET: Admin/Reviews/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var review = await _reviewService.GetReviewByIdAsync(id);
                if (review == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                    return RedirectToAction(nameof(Index));
                }

                return View(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review details for ID: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đánh giá.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Reviews/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var result = await _reviewService.ActivateReviewAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đánh giá đã được kích hoạt thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể kích hoạt đánh giá.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating review: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi kích hoạt đánh giá.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Reviews/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var result = await _reviewService.DeactivateReviewAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đánh giá đã được vô hiệu hóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể vô hiệu hóa đánh giá.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating review: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi vô hiệu hóa đánh giá.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Reviews/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var review = await _reviewService.GetReviewByIdAsync(id);
                if (review == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                    return RedirectToAction(nameof(Index));
                }

                return View(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review for delete: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đánh giá.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _reviewService.DeleteReviewAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đánh giá đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá để xóa.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa đánh giá.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // GET: Admin/Reviews/CarSummary/5
        public async Task<IActionResult> CarSummary(int carId)
        {
            try
            {
                var car = await _carService.GetCarByIdAsync(carId);
                if (car == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe.";
                    return RedirectToAction(nameof(Index));
                }

                var reviews = await _reviewService.GetReviewsByCarAsync(carId);
                var averageRating = await _reviewService.GetAverageRatingByCarAsync(carId);
                var totalReviews = await _reviewService.GetReviewCountByCarAsync(carId);

                var ratingGroups = reviews.GroupBy(r => r.Rating).ToDictionary(g => g.Key, g => g.Count());

                var recentReviews = reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToList();

                var recommendedCount = reviews.Count(r => r.IsRecommended);
                var recommendationPercentage = totalReviews > 0 ? (double)recommendedCount / totalReviews * 100 : 0;

                // Sử dụng ViewBag để truyền dữ liệu tóm tắt
                ViewBag.CarId = carId;
                ViewBag.CarName = car.Name;
                ViewBag.AverageRating = averageRating;
                ViewBag.TotalReviews = totalReviews;
                ViewBag.FiveStarCount = ratingGroups.GetValueOrDefault(5, 0);
                ViewBag.FourStarCount = ratingGroups.GetValueOrDefault(4, 0);
                ViewBag.ThreeStarCount = ratingGroups.GetValueOrDefault(3, 0);
                ViewBag.TwoStarCount = ratingGroups.GetValueOrDefault(2, 0);
                ViewBag.OneStarCount = ratingGroups.GetValueOrDefault(1, 0);
                ViewBag.RecommendationPercentage = recommendationPercentage;

                return View(recentReviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car review summary for car: {CarId}", carId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải tóm tắt đánh giá xe.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}