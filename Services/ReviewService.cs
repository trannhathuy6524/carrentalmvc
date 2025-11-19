using carrentalmvc.Models;
using carrentalmvc.Repositories;

namespace carrentalmvc.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Review>> GetAllReviewsAsync()
        {
            return await _unitOfWork.Reviews.GetAllAsync();
        }

        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            return await _unitOfWork.Reviews.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Review>> GetReviewsByCarAsync(int carId)
        {
            return await _unitOfWork.Reviews.GetReviewsByCarAsync(carId);
        }

        public async Task<IEnumerable<Review>> GetReviewsByUserAsync(string userId)
        {
            return await _unitOfWork.Reviews.GetReviewsByUserAsync(userId);
        }

        public async Task<IEnumerable<Review>> GetActiveReviewsAsync()
        {
            return await _unitOfWork.Reviews.GetActiveReviewsAsync();
        }

        public async Task<Review> CreateReviewAsync(Review review)
        {
            try
            {
                // Kiểm tra user đã thuê xe này chưa
                if (!await CanUserReviewCarAsync(review.UserId, review.CarId))
                {
                    throw new InvalidOperationException("Bạn chỉ có thể đánh giá xe mà bạn đã thuê.");
                }

                // Kiểm tra user đã review xe này chưa
                var existingReviews = await _unitOfWork.Reviews.GetAsync(
                    r => r.UserId == review.UserId && r.CarId == review.CarId);

                if (existingReviews.Any())
                {
                    throw new InvalidOperationException("Bạn đã đánh giá xe này rồi.");
                }

                // Validate rating
                if (review.Rating < 1 || review.Rating > 5)
                {
                    throw new InvalidOperationException("Đánh giá phải từ 1 đến 5 sao.");
                }

                review.IsActive = true;
                review.CreatedAt = DateTime.UtcNow;
                review.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Reviews.AddAsync(review);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã tạo review mới: ID {ReviewId} cho xe {CarId}", review.ReviewId, review.CarId);
                return review;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo review cho xe ID: {CarId}", review.CarId);
                throw;
            }
        }

        public async Task<Review> UpdateReviewAsync(Review review)
        {
            try
            {
                var existingReview = await _unitOfWork.Reviews.GetByIdAsync(review.ReviewId);
                if (existingReview == null)
                {
                    throw new InvalidOperationException($"Không tìm thấy review với ID: {review.ReviewId}");
                }

                // Chỉ cho phép user tự update review của mình
                if (existingReview.UserId != review.UserId)
                {
                    throw new UnauthorizedAccessException("Bạn chỉ có thể chỉnh sửa review của chính mình.");
                }

                // Validate rating
                if (review.Rating < 1 || review.Rating > 5)
                {
                    throw new InvalidOperationException("Đánh giá phải từ 1 đến 5 sao.");
                }

                existingReview.Rating = review.Rating;
                existingReview.Comment = review.Comment;
                existingReview.IsRecommended = review.IsRecommended;
                existingReview.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Reviews.Update(existingReview);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã cập nhật review ID: {ReviewId}", review.ReviewId);
                return existingReview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật review ID: {ReviewId}", review.ReviewId);
                throw;
            }
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            try
            {
                var review = await _unitOfWork.Reviews.GetByIdAsync(id);
                if (review == null)
                {
                    return false;
                }

                _unitOfWork.Reviews.Remove(review);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã xóa review ID: {ReviewId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa review ID: {ReviewId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateReviewAsync(int id)
        {
            try
            {
                var review = await _unitOfWork.Reviews.GetByIdAsync(id);
                if (review == null)
                {
                    return false;
                }

                review.IsActive = true;
                review.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Reviews.Update(review);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã kích hoạt review ID: {ReviewId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kích hoạt review ID: {ReviewId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateReviewAsync(int id)
        {
            try
            {
                var review = await _unitOfWork.Reviews.GetByIdAsync(id);
                if (review == null)
                {
                    return false;
                }

                review.IsActive = false;
                review.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Reviews.Update(review);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Đã vô hiệu hóa review ID: {ReviewId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi vô hiệu hóa review ID: {ReviewId}", id);
                throw;
            }
        }

        public async Task<double> GetAverageRatingByCarAsync(int carId)
        {
            return await _unitOfWork.Reviews.GetAverageRatingByCarAsync(carId);
        }

        public async Task<int> GetReviewCountByCarAsync(int carId)
        {
            return await _unitOfWork.Reviews.GetReviewCountByCarAsync(carId);
        }

        public async Task<(IEnumerable<Review> reviews, int totalCount)> GetPagedReviewsByCarAsync(
            int carId, int pageNumber = 1, int pageSize = 10)
        {
            return await _unitOfWork.Reviews.GetPagedReviewsByCarAsync(carId, pageNumber, pageSize);
        }

        public async Task<bool> CanUserReviewCarAsync(string userId, int carId)
        {
            // Kiểm tra user đã có rental completed cho xe này chưa
            var completedRentals = await _unitOfWork.Rentals.GetAsync(
                r => r.RenterId == userId &&
                     r.CarId == carId &&
                     r.Status == Models.Enums.RentalStatus.Completed);

            return completedRentals.Any();
        }

        public async Task<IEnumerable<Review>> GetReviewsByOwnerAsync(string ownerId)
        {
            try
            {
                // Lấy tất cả reviews của các xe thuộc về owner này
                var reviews = await _unitOfWork.Reviews.GetAsync(r =>
                    r.Car != null &&
                    r.Car.OwnerId == ownerId &&
                    r.IsActive);

                return reviews.OrderByDescending(r => r.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy reviews cho owner: {OwnerId}", ownerId);
                return Enumerable.Empty<Review>();
            }
        }
    }
}