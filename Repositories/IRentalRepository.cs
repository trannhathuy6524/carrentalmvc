using carrentalmvc.Models;
using carrentalmvc.Models.Enums;

namespace carrentalmvc.Repositories
{
    public interface IRentalRepository : IRepository<Rental>
    {
        Task<IEnumerable<Rental>> GetAllWithDetailsAsync();
        Task<IEnumerable<Rental>> GetRentalsByUserAsync(string userId);
        Task<IEnumerable<Rental>> GetRentalsByCarAsync(int carId);
        Task<IEnumerable<Rental>> GetRentalsByOwnerAsync(string ownerId);
        Task<IEnumerable<Rental>> GetRentalsByStatusAsync(RentalStatus status);
        Task<IEnumerable<Rental>> GetActiveRentalsAsync();
        Task<IEnumerable<Rental>> GetOverdueRentalsAsync();
        Task<Rental?> GetRentalWithDetailsAsync(int rentalId);
        Task<bool> IsCarAvailableAsync(int carId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<Rental>> GetRentalsInDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}