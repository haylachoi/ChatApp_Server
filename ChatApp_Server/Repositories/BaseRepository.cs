
using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.VisualBasic;
using System;
using System.Linq.Expressions;

namespace ChatApp_Server.Repositories
{
    public interface IBaseRepository<TEntity, TId> where TEntity : class where TId: notnull
    {

        Task<IEnumerable<TEntity>> GetAllAsync(IEnumerable<Expression<Func<TEntity, bool>>> filters = null!,
           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null!,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>> includes = null!,
           int? skip = null,
           int? take = null);
        Task<TEntity?> GetOne(IEnumerable<Expression<Func<TEntity, bool>>> filters = null!, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? includes = null);

        Task<TEntity?> GetByIdAsync(TId id);
        void Insert(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity Entity);
        void Delete(TId id);
        Task SaveAsync();

    }
    public abstract class BaseRepository<TEntity, TId>: IBaseRepository<TEntity, TId> where TEntity : class where TId : notnull
    {
        protected readonly ChatAppContext _context;

        public BaseRepository(ChatAppContext context)
        {
            _context = context;
        }
        
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(IEnumerable<Expression<Func<TEntity, bool>>> filters = null!,
           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? includes = null,
           int? skip = null,
           int? take = null)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            if (filters != null && filters.Count() > 0)
            {
                query = filters.Aggregate(query, (acc, next) => acc.Where(next));
            }

            if (includes != null)
            {
                query = includes(query);
            }


            if (orderBy != null)
            {
                query = orderBy(query);
            }
            if (skip!= null)
            {
                query = query.Skip(skip.Value);
            }
            if (take!= null)
            {
                query = query.Take(take.Value);
            }

            return await query.AsNoTracking().ToListAsync();
        }
        public virtual async Task<TEntity?> GetOne(IEnumerable<Expression<Func<TEntity, bool>>> filters = null!, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? includes = null)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            if (filters != null && filters.Count() > 0)
            {
                query = filters.Aggregate(query, (acc, next) => acc.Where(next));
            }
            if (includes != null)
            {
                query = includes(query);
            }
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }
        public virtual async Task<TEntity?> GetByIdAsync(TId id)
        {
            return await _context.Set<TEntity>().FindAsync(id);
        }
        public void Insert(TEntity entity) => _context.Set<TEntity>().Add(entity);
       
        public void Update(TEntity entity) => _context.Set<TEntity>().Update(entity);
       

        public void Delete(TEntity entity) => _context.Set<TEntity>().Remove(entity);
       
        public async Task SaveAsync() => await _context.SaveChangesAsync();

        public virtual void Delete(TId id) => _context.Remove(id);

    }
}
