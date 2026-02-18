using System.Linq.Expressions;

using BuildingBlocks.Abstractions;

namespace BuildingBlocks;

public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly IDbContext _context;

    protected Repository(IDbContext context)
    {
        _context = context;
    }

    public abstract Task<IReadOnlyList<TEntity>> GetManyAsync(Expression<Func<TEntity, bool>> expression,
        Expression<Func<TEntity, object>>? orderByExpression = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default);

    public abstract Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> expression,
        CancellationToken cancellationToken = default);

    public abstract Task AddAsync(TEntity entity,
        CancellationToken cancellationToken = default);
    public abstract Task AddAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);
    public abstract void Update(TEntity entity);
    public abstract void Update(IEnumerable<TEntity> entities);
    public abstract void Delete(TEntity entity);
    public abstract void Delete(IEnumerable<TEntity> entities);
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
}
