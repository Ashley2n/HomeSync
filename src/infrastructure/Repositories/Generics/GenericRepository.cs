using domain.Exceptions;
using infrastructure.Data;
using infrastructure.Interfaces.Generics;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Repositories.Generics;

public class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    protected DbSet<T> _db;
    protected GenericRepository(AppDbContext context)
    {
        _context = context;
        _db = _context.Set<T>();
    }

    public async Task<List<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.ToListAsync(ct);
    }

    public async Task<T> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.FindAsync([id], ct) ?? throw new NotFoundException(typeof(T).Name, id);
    }

    public async Task CreateAsync(T entity, CancellationToken ct = default)
    {
        await _db.AddAsync(entity, ct);
    }

    public Task UpdateAsync(T entity, Guid id, CancellationToken ct = default)
    {
         _db.Update(entity);
         return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetAsync(id, ct);
        _db.Remove(entity);
    }
    public async Task SaveDbChangesAsync(CancellationToken ct = default) => 
    await _context.SaveChangesAsync(ct);
    
}