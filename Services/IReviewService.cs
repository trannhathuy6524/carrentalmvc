using carrentalmvc.Models;

namespace carrentalmvc.Services
{
    public interface IReviewService
    {
        Task<IEnumerable<Review>> GetAllReviewsAsync();
        Task<Review?> GetReviewByIdAsync(int id);
        Task<IEnumerable<Review>> GetReviewsByCarAsync(int carId);
        Task<IEnumerable<Review>> GetReviewsByUserAsync(string userId);
        Task<IEnumerable<Review>> GetActiveReviewsAsync();
        Task<Review> CreateReviewAsync(Review review);
        Task<Review> UpdateReviewAsync(Review review);
        Task<bool> DeleteReviewAsync(int id);
        Task<bool> ActivateReviewAsync(int id);
        Task<bool> DeactivateReviewAsync(int id);
        Task<double> GetAverageRatingByCarAsync(int carId);
        Task<int> GetReviewCountByCarAsync(int carId);
        Task<(IEnumerable<Review> reviews, int totalCount)> GetPagedReviewsByCarAsync(
            int carId, int pageNumber = 1, int pageSize = 10);
        Task<bool> CanUserReviewCarAsync(string userId, int carId);
        Task<IEnumerable<Review>> GetReviewsByOwnerAsync(string ownerId);
    }
}