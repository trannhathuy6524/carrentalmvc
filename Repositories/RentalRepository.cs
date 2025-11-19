using carrentalmvc.Data;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class RentalRepository : Repository<Rental>, IRentalRepository
    {
        public RentalRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Rental>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.Car)
                    .ThenInclude(c => c.Category)
                .Include(r => r.Car)
                    .ThenInclude(c => c.CarImages.Where(img => img.IsPrimary))
                .Include(r => r.Renter)
                .Include(r => r.Payments)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Rental>> GetRentalsByUserAsync(string userId)
        {
            return await _dbSet
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.Car)
                    .ThenInclude(c => c.CarImages.Where(img => img.IsPrimary))
                .Where(r => r.RenterId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Rental>> GetRentalsByCarAsync(int carId)
        {
            return await _dbSet
                .Include(r => r.Renter)
                .Where(r => r.CarId == carId)
                .OrderByDescending(r => r.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Rental>> GetRentalsByStatusAsync(RentalStatus status)
        {
            return await _dbSet
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.Renter)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Rental>> GetActiveRentalsAsync()
        {
            return await _dbSet
                .Include(r => r.Car)
                .Include(r => r.Renter)
                .Where(r => r.Status == RentalStatus.Active)
                .ToListAsync();
        }

        public async Task<IEnumerable<Rental>> GetOverdueRentalsAsync()
        {
            var today = DateTime.Today;
            return await _dbSet
                .Include(r => r.Car)
                .Include(r => r.Renter)
                .Where(r => r.Status == RentalStatus.Active && r.EndDate < today)
                .ToListAsync();
        }

        public async Task<Rental?> GetRentalWithDetailsAsync(int rentalId)
        {
            return await _dbSet
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.Car)
                    .ThenInclude(c => c.Category)
                .Include(r => r.Car)
                    .ThenInclude(c => c.CarImages)
                .Include(r => r.Renter)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.RentalId == rentalId);
        }

        public async Task<bool> IsCarAvailableAsync(int carId, DateTime startDate, DateTime endDate)
        {
            var conflictingRentals = await _dbSet
                .Where(r => r.CarId == carId &&
                           r.Status != RentalStatus.Cancelled &&
                           r.Status != RentalStatus.Completed &&
                           ((startDate >= r.StartDate && startDate <= r.EndDate) ||
                            (endDate >= r.StartDate && endDate <= r.EndDate) ||
                            (startDate <= r.StartDate && endDate >= r.EndDate)))
                .AnyAsync();

            return !conflictingRentals;
        }

        public async Task<IEnumerable<Rental>> GetRentalsInDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(r => r.Car)
                .Include(r => r.Renter)
                .Where(r => r.StartDate >= startDate && r.StartDate <= endDate)
                .OrderBy(r => r.StartDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Where(r => r.Status == RentalStatus.Completed);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            return await query.SumAsync(r => r.TotalPrice);
        }

        public async Task<IEnumerable<Rental>> GetRentalsByOwnerAsync(string ownerId)
        {
            return await _dbSet
                .Include(r => r.Car)
                    .ThenInclude(c => c.Brand)
                .Include(r => r.Car)
                    .ThenInclude(c => c.Category)
                .Include(r => r.Renter)
                .Where(r => r.Car != null && r.Car.OwnerId == ownerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

    }
}