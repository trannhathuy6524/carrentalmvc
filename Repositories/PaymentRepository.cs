using carrentalmvc.Data;
using carrentalmvc.Models;
using carrentalmvc.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace carrentalmvc.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Payment>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                        .ThenInclude(c => c.Brand)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                        .ThenInclude(c => c.Brand)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .FirstOrDefaultAsync(p => p.PaymentId == id);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByRentalAsync(int rentalId)
        {
            return await _dbSet
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .Where(p => p.RentalId == rentalId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            return await _dbSet
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByMethodAsync(PaymentMethod method)
        {
            return await _dbSet
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .Where(p => p.PaymentMethod == method)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaymentsInDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                        .ThenInclude(c => c.Brand)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Where(p => p.Status == PaymentStatus.Completed && p.Amount > 0);

            if (startDate.HasValue)
                query = query.Where(p => p.PaymentDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.PaymentDate <= endDate.Value);

            var result = await query.SumAsync(p => p.Amount);
            return result;
        }

        public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId)
        {
            return await _dbSet
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Car)
                .Include(p => p.Rental)
                    .ThenInclude(r => r.Renter)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }
    }
}