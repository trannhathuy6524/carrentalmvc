using carrentalmvc.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace carrentalmvc.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Brands = new BrandRepository(_context);
            Categories = new CategoryRepository(_context);
            Cars = new CarRepository(_context);
            Features = new FeatureRepository(_context);
            Rentals = new RentalRepository(_context);
            Payments = new PaymentRepository(_context);
            Reviews = new ReviewRepository(_context);
            Model3DTemplates = new Model3DTemplateRepository(_context);
            PaymentDistributions = new PaymentDistributionRepository(_context);
        }

        public IBrandRepository Brands { get; private set; }
        public ICategoryRepository Categories { get; private set; }
        public ICarRepository Cars { get; private set; }
        public IFeatureRepository Features { get; private set; }
        public IRentalRepository Rentals { get; private set; }
        public IPaymentRepository Payments { get; private set; }
        public IReviewRepository Reviews { get; private set; }
        public IModel3DTemplateRepository Model3DTemplates { get; private set; }
        public IPaymentDistributionRepository PaymentDistributions { get; private set; }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}