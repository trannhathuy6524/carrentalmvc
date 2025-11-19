using carrentalmvc.Models;
using carrentalmvc.Models.Enums;

namespace carrentalmvc.Repositories
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetPaymentsByRentalAsync(int rentalId);
        Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);
        Task<IEnumerable<Payment>> GetPaymentsByMethodAsync(PaymentMethod method);
        Task<IEnumerable<Payment>> GetPaymentsInDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId);

        // Thêm methods mới
        Task<IEnumerable<Payment>> GetAllWithDetailsAsync();
        Task<Payment?> GetByIdWithDetailsAsync(int id);
    }
}