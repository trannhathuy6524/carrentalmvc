using carrentalmvc.Models;
using carrentalmvc.Models.Enums;

namespace carrentalmvc.Services
{
    public interface IPaymentDistributionService
    {
        /// <summary>
        /// Create distribution records for a payment (Platform, Owner, Driver)
        /// </summary>
        Task<bool> CreateDistributionAsync(int paymentId);

        /// <summary>
        /// Get all distributions for a payment
        /// </summary>
        Task<IEnumerable<PaymentDistribution>> GetDistributionsByPaymentAsync(int paymentId);

        /// <summary>
        /// Get all distributions for a recipient (user)
        /// </summary>
        Task<IEnumerable<PaymentDistribution>> GetDistributionsByRecipientAsync(string recipientId);

        /// <summary>
        /// Get pending distributions
        /// </summary>
        Task<IEnumerable<PaymentDistribution>> GetPendingDistributionsAsync();

        /// <summary>
        /// Mark a distribution as completed
        /// </summary>
        Task<bool> MarkDistributionCompletedAsync(int distributionId, string transactionReference);

        /// <summary>
        /// Mark a distribution as failed
        /// </summary>
        Task<bool> MarkDistributionFailedAsync(int distributionId, string errorMessage);

        /// <summary>
        /// Get total pending amount for a recipient
        /// </summary>
        Task<decimal> GetPendingAmountForRecipientAsync(string recipientId);

        /// <summary>
        /// Get total completed amount for a recipient
        /// </summary>
        Task<decimal> GetCompletedAmountForRecipientAsync(string recipientId);
    }
}
