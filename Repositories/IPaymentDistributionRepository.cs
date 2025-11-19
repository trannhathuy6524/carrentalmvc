using carrentalmvc.Models;
using carrentalmvc.Models.Enums;

namespace carrentalmvc.Repositories
{
    public interface IPaymentDistributionRepository : IRepository<PaymentDistribution>
    {
        Task<IEnumerable<PaymentDistribution>> GetByPaymentIdAsync(int paymentId);
        Task<IEnumerable<PaymentDistribution>> GetByRecipientIdAsync(string recipientId);
        Task<IEnumerable<PaymentDistribution>> GetByStatusAsync(PaymentDistributionStatus status);
        Task<IEnumerable<PaymentDistribution>> GetPendingDistributionsAsync();
    }
}
