using carrentalmvc.Models;
using carrentalmvc.Models.Enums;

namespace carrentalmvc.Services
{
    public interface IRentalService
    {
        Task<IEnumerable<Rental>> GetAllRentalsAsync();
        Task<Rental?> GetRentalByIdAsync(int id);
        Task<Rental?> GetRentalWithDetailsAsync(int id);
        Task<IEnumerable<Rental>> GetRentalsByUserAsync(string userId);
        Task<IEnumerable<Rental>> GetRentalsByCarAsync(int carId);
        Task<IEnumerable<Rental>> GetRentalsByStatusAsync(RentalStatus status);
        Task<IEnumerable<Rental>> GetActiveRentalsAsync();
        Task<IEnumerable<Rental>> GetOverdueRentalsAsync();
        Task<Rental> CreateRentalAsync(Rental rental);
        Task<Rental> UpdateRentalAsync(Rental rental);
        Task<bool> CancelRentalAsync(int rentalId, string reason = "");
        Task<bool> ConfirmRentalAsync(int rentalId);
        Task<bool> StartRentalAsync(int rentalId);
        Task<bool> CompleteRentalAsync(int rentalId, decimal? damageFee = null, string? notes = null);
        Task<bool> IsCarAvailableAsync(int carId, DateTime startDate, DateTime endDate);
        Task<decimal> CalculateRentalPriceAsync(int carId, DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<Rental>> GetRentalsInDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> ProcessOverdueRentalsAsync();
        Task<IEnumerable<Rental>> GetRentalsByOwnerAsync(string ownerId);
        Task<decimal> CalculateLateFeeAsync(int rentalId);

    }
}