namespace infrastructure.Interfaces.Generics;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync(CancellationToken ct  = default);
    Task<T> GetAsync(Guid id, CancellationToken ct  = default);
    Task CreateAsync(T entity,  CancellationToken ct  = default);
    Task UpdateAsync(T entity, Guid id,  CancellationToken ct  = default);
    Task DeleteAsync(Guid id,  CancellationToken ct  = default);
    Task SaveDbChangesAsync(CancellationToken ct  = default);
}