using domain.Exceptions;
using infrastructure.Data;
using infrastructure.Interfaces.Generics;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Repositories.Generics;

public class GenericRepository<T> : IRepository<T> where T : class
{
    private AppDbContext _context;
    protected DbSet<T> _db;
    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _db = _context.Set<T>();
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _db.ToListAsync();
    }

    public async Task<T> GetAsync(Guid id)
    {
        return await _db.FindAsync(id) ?? throw new NotFoundException(typeof(T).Name, id);
    }

    public async Task CreateAsync(T entity)
    {
        await _db.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _db.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        T? entity = await _db.FindAsync(id);
        if (entity != null)
        {
            _db.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}