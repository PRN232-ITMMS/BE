using System.Linq.Expressions;
using InfertilityTreatment.Entity.Common;

namespace InfertilityTreatment.Data.Repositories.Interfaces
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        // Basic CRUD Operations
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Advanced Operations
        Task<int> CountAsync();
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<T>> FindWithIncludeAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);
        Task<T?> GetByIdWithIncludeAsync(int id, params Expression<Func<T, object>>[] includes);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    }
}