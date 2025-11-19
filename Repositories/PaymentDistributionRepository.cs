using carrentalmvc.Data;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class PaymentDistributionRepository : Repository<PaymentDistribution>, IPaymentDistributionRepository
    {
        public PaymentDistributionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PaymentDistribution>> GetByPaymentIdAsync(int paymentId)
        {
            return await _context.PaymentDistributions
                .Include(pd => pd.Payment)
                .Include(pd => pd.Recipient)
                .Where(pd => pd.PaymentId == paymentId)
                .OrderBy(pd => pd.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PaymentDistribution>> GetByRecipientIdAsync(string recipientId)
        {
            return await _context.PaymentDistributions
                .Include(pd => pd.Payment)
                    .ThenInclude(p => p.Rental)
                .Where(pd => pd.RecipientId == recipientId)
                .OrderByDescending(pd => pd.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PaymentDistribution>> GetByStatusAsync(PaymentDistributionStatus status)
        {
            return await _context.PaymentDistributions
                .Include(pd => pd.Payment)
                .Include(pd => pd.Recipient)
                .Where(pd => pd.Status == status)
                .OrderBy(pd => pd.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PaymentDistribution>> GetPendingDistributionsAsync()
        {
            return await GetByStatusAsync(PaymentDistributionStatus.Pending);
        }
    }
}
