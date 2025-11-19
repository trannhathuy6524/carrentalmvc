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
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IRentalService _rentalService;
        private readonly ICarService _carService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(
            IReviewService reviewService,
            IRentalService rentalService,
            ICarService carService,
            UserManager<ApplicationUser> userManager,
            ILogger<ReviewsController> logger)
        {
            _reviewService = reviewService;
            _rentalService = rentalService;
            _carService = carService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Customer/Reviews
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var myReviews = await _reviewService.GetReviewsByUserAsync(user.Id);

                var totalCount = myReviews.Count();
                var pagedReviews = myReviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Pass pagination to ViewBag
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < ViewBag.TotalPages;

                return View(pagedReviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews for customer: {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đánh giá.";

                ViewBag.PageNumber = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = 0;
                ViewBag.TotalPages = 0;
                ViewBag.HasPreviousPage = false;
                ViewBag.HasNextPage = false;

                return View(new List<Review>());
            }
        }

        // GET: Customer/Reviews/Create/5 (CarId)
        public async Task<IActionResult> Create(int carId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Check if user has rented this car and completed the rental
                var userRentals = await _rentalService.GetRentalsByUserAsync(user.Id);
                var completedRental = userRentals.FirstOrDefault(r =>
                    r.CarId == carId && r.Status == RentalStatus.Completed);

                if (completedRental == null)
                {
                    TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá xe sau khi hoàn thành thuê xe.";
                    return RedirectToAction("Index", "Rentals");
                }

                // Check if user already reviewed this car
                var existingReviews = await _reviewService.GetReviewsByUserAsync(user.Id);
                var existingReview = existingReviews.FirstOrDefault(r => r.CarId == carId);

                if (existingReview != null)
                {
                    TempData["InfoMessage"] = "Bạn đã đánh giá xe này rồi. Bạn có thể chỉnh sửa đánh giá.";
                    return RedirectToAction(nameof(Edit), new { id = existingReview.ReviewId });
                }

                var car = await _carService.GetCarWithDetailsAsync(carId);
                if (car == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe.";
                    return RedirectToAction("Index", "Rentals");
                }

                // Get car image
                var primaryImage = car.CarImages?.FirstOrDefault(img => img.IsPrimary);
                var carImageUrl = primaryImage?.ImageUrl ?? car.CarImages?.FirstOrDefault()?.ImageUrl;

                // Pass data to ViewBag
                ViewBag.CarImageUrl = carImageUrl;
                ViewBag.BrandName = car.Brand?.Name ?? "N/A";
                ViewBag.HasUserRentedCar = true;
                ViewBag.CompletedRentalId = completedRental.RentalId;

                return View(car);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create review for car: {CarId}", carId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang đánh giá.";
                return RedirectToAction("Index", "Rentals");
            }
        }

        // POST: Customer/Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int carId,
            int rating,
            string? comment,
            bool isRecommended)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    TempData["ErrorMessage"] = "Đánh giá phải từ 1 đến 5 sao.";
                    return RedirectToAction(nameof(Create), new { carId });
                }

                // Check if user has completed rental for this car
                var userRentals = await _rentalService.GetRentalsByUserAsync(user.Id);
                var completedRental = userRentals.FirstOrDefault(r =>
                    r.CarId == carId && r.Status == RentalStatus.Completed);

                if (completedRental == null)
                {
                    TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá xe sau khi hoàn thành thuê xe.";
                    return RedirectToAction("Index", "Rentals");
                }

                // Check if already reviewed
                var existingReviews = await _reviewService.GetReviewsByUserAsync(user.Id);
                var existingReview = existingReviews.FirstOrDefault(r => r.CarId == carId);

                if (existingReview != null)
                {
                    TempData["ErrorMessage"] = "Bạn đã đánh giá xe này rồi.";
                    return RedirectToAction(nameof(Edit), new { id = existingReview.ReviewId });
                }

                var review = new Review
                {
                    CarId = carId,
                    UserId = user.Id,
                    Rating = rating,
                    Comment = comment,
                    IsRecommended = isRecommended,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _reviewService.CreateReviewAsync(review);

                TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công. Cảm ơn bạn đã đóng góp ý kiến!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for car: {CarId}", carId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi đánh giá.";
                return RedirectToAction(nameof(Create), new { carId });
            }
        }

        // GET: Customer/Reviews/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var review = await _reviewService.GetReviewByIdAsync(id);

                if (review == null || review.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá hoặc bạn không có quyền chỉnh sửa.";
                    return RedirectToAction(nameof(Index));
                }

                var car = await _carService.GetCarWithDetailsAsync(review.CarId);

                if (car == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin xe.";
                    return RedirectToAction(nameof(Index));
                }

                // Get car image
                var primaryImage = car.CarImages?.FirstOrDefault(img => img.IsPrimary);
                var carImageUrl = primaryImage?.ImageUrl ?? car.CarImages?.FirstOrDefault()?.ImageUrl;

                // Pass data to ViewBag
                ViewBag.CarName = car.Name;
                ViewBag.CarImageUrl = carImageUrl;
                ViewBag.BrandName = car.Brand?.Name ?? "N/A";
                ViewBag.HasUserRentedCar = true;

                return View(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit review: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đánh giá.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Reviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int rating,
            string? comment,
            bool isRecommended)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var existingReview = await _reviewService.GetReviewByIdAsync(id);

                if (existingReview == null || existingReview.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá hoặc bạn không có quyền chỉnh sửa.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    TempData["ErrorMessage"] = "Đánh giá phải từ 1 đến 5 sao.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                existingReview.Rating = rating;
                existingReview.Comment = comment;
                existingReview.IsRecommended = isRecommended;
                existingReview.UpdatedAt = DateTime.UtcNow;

                await _reviewService.UpdateReviewAsync(existingReview);

                TempData["SuccessMessage"] = "Đánh giá đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật đánh giá.";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // GET: Customer/Reviews/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var review = await _reviewService.GetReviewByIdAsync(id);

                if (review == null || review.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa.";
                    return RedirectToAction(nameof(Index));
                }

                return View(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete review: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đánh giá.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var review = await _reviewService.GetReviewByIdAsync(id);

                if (review == null || review.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _reviewService.DeleteReviewAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đánh giá đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa đánh giá này.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa đánh giá.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Customer/Reviews/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var review = await _reviewService.GetReviewByIdAsync(id);

                if (review == null || review.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đánh giá hoặc bạn không có quyền xem.";
                    return RedirectToAction(nameof(Index));
                }

                var car = await _carService.GetCarWithDetailsAsync(review.CarId);

                // Get car image
                var primaryImage = car?.CarImages?.FirstOrDefault(img => img.IsPrimary);
                var carImageUrl = primaryImage?.ImageUrl ?? car?.CarImages?.FirstOrDefault()?.ImageUrl;

                // Pass data to ViewBag
                ViewBag.CarImageUrl = carImageUrl;
                ViewBag.BrandName = car?.Brand?.Name ?? "N/A";

                return View(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review details: {ReviewId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đánh giá.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}