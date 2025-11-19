using carrentalmvc.Data.Constants;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using carrentalmvc.Repositories;

namespace carrentalmvc.Services
{
    public class PaymentDistributionService : IPaymentDistributionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentDistributionService> _logger;

        // Platform ID constant - can be moved to config
        private const string PLATFORM_RECIPIENT_ID = "PLATFORM_SYSTEM";

        public PaymentDistributionService(
            IUnitOfWork unitOfWork,
            ILogger<PaymentDistributionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> CreateDistributionAsync(int paymentId)
        {
            try
            {
                // Get payment with rental details
                var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
                    return false;
                }

                // Check if distribution already exists
                var existingDistributions = await GetDistributionsByPaymentAsync(paymentId);
                if (existingDistributions.Any())
                {
                    _logger.LogInformation("Distribution already exists for payment: {PaymentId}", paymentId);
                    return true; // Already distributed
                }

                // Get rental to determine recipients
                var rental = await _unitOfWork.Rentals.GetByIdAsync(payment.RentalId);
                if (rental == null)
                {
                    _logger.LogError("Rental not found for payment: {PaymentId}", paymentId);
                    return false;
                }

                // Calculate breakdown
                var driverFee = rental.ActualDriverFee ?? 0m;
                var breakdown = PlatformConstants.GetRevenueBreakdown(payment.Amount, driverFee);

                var distributions = new List<PaymentDistribution>();

                // 1. Platform Distribution
                distributions.Add(new PaymentDistribution
                {
                    PaymentId = paymentId,
                    RecipientId = PLATFORM_RECIPIENT_ID,
                    RecipientType = RecipientType.Platform,
                    Amount = breakdown.PlatformFee,
                    Status = PaymentDistributionStatus.Pending,
                    Notes = $"Platform commission ({PlatformConstants.COMMISSION_RATE:P0})",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                // 2. Car Owner Distribution
                var car = await _unitOfWork.Cars.GetByIdAsync(rental.CarId);
                if (car != null && !string.IsNullOrEmpty(car.OwnerId))
                {
                    distributions.Add(new PaymentDistribution
                    {
                        PaymentId = paymentId,
                        RecipientId = car.OwnerId,
                        RecipientType = RecipientType.CarOwner,
                        Amount = breakdown.OwnerRevenue,
                        Status = PaymentDistributionStatus.Pending,
                        Notes = $"Car owner revenue (Rental + Delivery - Commission)",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                // 3. Driver Distribution (if applicable)
                if (rental.RequiresDriver && !string.IsNullOrEmpty(rental.DriverId) && driverFee > 0)
                {
                    distributions.Add(new PaymentDistribution
                    {
                        PaymentId = paymentId,
                        RecipientId = rental.DriverId,
                        RecipientType = RecipientType.Driver,
                        Amount = driverFee,
                        Status = PaymentDistributionStatus.Pending,
                        Notes = $"Driver service fee",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                // Save all distributions
                foreach (var distribution in distributions)
                {
                    await _unitOfWork.PaymentDistributions.AddAsync(distribution);
                }

                await _unitOfWork.SaveAsync();

                _logger.LogInformation(
                    "Created {Count} distributions for payment {PaymentId}: Platform={Platform}, Owner={Owner}, Driver={Driver}",
                    distributions.Count, paymentId, breakdown.PlatformFee, breakdown.OwnerRevenue, driverFee);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating distribution for payment: {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<IEnumerable<PaymentDistribution>> GetDistributionsByPaymentAsync(int paymentId)
        {
            return await _unitOfWork.PaymentDistributions.GetByPaymentIdAsync(paymentId);
        }

        public async Task<IEnumerable<PaymentDistribution>> GetDistributionsByRecipientAsync(string recipientId)
        {
            return await _unitOfWork.PaymentDistributions.GetByRecipientIdAsync(recipientId);
        }

        public async Task<IEnumerable<PaymentDistribution>> GetPendingDistributionsAsync()
        {
            return await _unitOfWork.PaymentDistributions.GetPendingDistributionsAsync();
        }

        public async Task<bool> MarkDistributionCompletedAsync(int distributionId, string transactionReference)
        {
            try
            {
                var distribution = await _unitOfWork.PaymentDistributions.GetByIdAsync(distributionId);
                if (distribution == null)
                {
                    return false;
                }

                distribution.Status = PaymentDistributionStatus.Completed;
                distribution.ProcessedAt = DateTime.UtcNow;
                distribution.TransactionReference = transactionReference;
                distribution.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.PaymentDistributions.Update(distribution);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation(
                    "Distribution {DistributionId} marked as completed. Recipient: {RecipientId}, Amount: {Amount}",
                    distributionId, distribution.RecipientId, distribution.Amount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking distribution as completed: {DistributionId}", distributionId);
                return false;
            }
        }

        public async Task<bool> MarkDistributionFailedAsync(int distributionId, string errorMessage)
        {
            try
            {
                var distribution = await _unitOfWork.PaymentDistributions.GetByIdAsync(distributionId);
                if (distribution == null)
                {
                    return false;
                }

                distribution.Status = PaymentDistributionStatus.Failed;
                distribution.ErrorMessage = errorMessage;
                distribution.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.PaymentDistributions.Update(distribution);
                await _unitOfWork.SaveAsync();

                _logger.LogWarning(
                    "Distribution {DistributionId} marked as failed. Error: {Error}",
                    distributionId, errorMessage);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking distribution as failed: {DistributionId}", distributionId);
                return false;
            }
        }

        public async Task<decimal> GetPendingAmountForRecipientAsync(string recipientId)
        {
            var distributions = await _unitOfWork.PaymentDistributions.GetAsync(
                pd => pd.RecipientId == recipientId && pd.Status == PaymentDistributionStatus.Pending);

            return distributions.Sum(pd => pd.Amount);
        }

        public async Task<decimal> GetCompletedAmountForRecipientAsync(string recipientId)
        {
            var distributions = await _unitOfWork.PaymentDistributions.GetAsync(
                pd => pd.RecipientId == recipientId && pd.Status == PaymentDistributionStatus.Completed);

            return distributions.Sum(pd => pd.Amount);
        }
    }
}
