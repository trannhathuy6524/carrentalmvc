using carrentalmvc.Data;
using carrentalmvc.Models;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Review>> GetReviewsByCarAsync(int carId)
        {
            return await _dbSet
                .Include(r => r.User)
                .Where(r => r.CarId == carId && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetReviewsByUserAsync(string userId)
        {
            return await _dbSet
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetActiveReviewsAsync()
        {
            return await _dbSet
                .Include(r => r.Car)
                .Include(r => r.User)
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingByCarAsync(int carId)
        {
            var reviews = await _dbSet
                .Where(r => r.CarId == carId && r.IsActive)
                .ToListAsync();

            return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        }

        public async Task<int> GetReviewCountByCarAsync(int carId)
        {
            return await _dbSet
                .CountAsync(r => r.CarId == carId && r.IsActive);
        }

        public async Task<(IEnumerable<Review> reviews, int totalCount)> GetPagedReviewsByCarAsync(
            int carId, int pageNumber = 1, int pageSize = 10)
        {
            var query = _dbSet
                .Include(r => r.User)
                .Where(r => r.CarId == carId && r.IsActive);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }
    }
}