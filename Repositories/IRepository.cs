using System.Linq.Expressions;

namespace carrentalmvc.Repositories
{
    public interface IRepository<T> where T : class
    {
        // Đọc dữ liệu
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string includeProperties = "");
        Task<T?> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>>? filter = null,
            string includeProperties = "");

        // Ghi dữ liệu
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        // Kiểm tra tồn tại
        Task<bool> ExistsAsync(int id);
        Task<bool> AnyAsync(Expression<Func<T, bool>> filter);

        // Đếm
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

        // Phân trang
        Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string includeProperties = "");
    }
}