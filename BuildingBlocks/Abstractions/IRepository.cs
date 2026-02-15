using System.Linq.Expressions;

namespace BuildingBlocks.Abstractions;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<IReadOnlyList<TEntity>> GetManyAsync(Expression<Func<TEntity, bool>> expression,
        Expression<Func<TEntity, object>>? orderByExpression = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default);
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Update(IEnumerable<TEntity> entities);
    void Delete(TEntity entity);
    void Delete(IEnumerable<TEntity> entities);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
