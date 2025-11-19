using carrentalmvc.Models;
using carrentalmvc.Models.Enums;

namespace carrentalmvc.Services
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetAllPaymentsAsync();
        Task<Payment?> GetPaymentByIdAsync(int id);
        Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId);
        Task<IEnumerable<Payment>> GetPaymentsByRentalAsync(int rentalId);
        Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);
        Task<Payment> CreatePaymentAsync(Payment payment);
        Task<Payment> UpdatePaymentAsync(Payment payment);
        Task<bool> ProcessPaymentAsync(int paymentId, string transactionId);
        Task<bool> RefundPaymentAsync(int paymentId, decimal refundAmount, string reason);
        Task<Payment> CreateDepositPaymentAsync(int rentalId, decimal amount, PaymentMethod method);
        Task<Payment> CreateRentalPaymentAsync(int rentalId, decimal amount, PaymentMethod method);
        Task<decimal> GetTotalPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<Payment>> GetPaymentsInDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> ValidatePaymentAsync(Payment payment);

        // Thêm các methods mới
        Task<bool> MarkPaymentCompletedAsync(int paymentId);
        Task<bool> MarkPaymentFailedAsync(int paymentId);
        Task<bool> CancelPaymentAsync(int paymentId, string reason = "");
        Task<decimal> GetTotalPaymentsByOwnerAsync(string ownerId, DateTime? startDate = null, DateTime? endDate = null);
    }
}