namespace carrentalmvc.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IBrandRepository Brands { get; }
        ICategoryRepository Categories { get; }
        ICarRepository Cars { get; }
        IFeatureRepository Features { get; }
        IRentalRepository Rentals { get; }
        IPaymentRepository Payments { get; }
        IReviewRepository Reviews { get; }
        IPaymentDistributionRepository PaymentDistributions { get; }
        IModel3DTemplateRepository Model3DTemplates { get; }

        Task<int> SaveAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}