using carrentalmvc.Models;

namespace carrentalmvc.Repositories
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<IEnumerable<Review>> GetReviewsByCarAsync(int carId);
        Task<IEnumerable<Review>> GetReviewsByUserAsync(string userId);
        Task<IEnumerable<Review>> GetActiveReviewsAsync();
        Task<double> GetAverageRatingByCarAsync(int carId);
        Task<int> GetReviewCountByCarAsync(int carId);
        Task<(IEnumerable<Review> reviews, int totalCount)> GetPagedReviewsByCarAsync(
            int carId, int pageNumber = 1, int pageSize = 10);
    }
}