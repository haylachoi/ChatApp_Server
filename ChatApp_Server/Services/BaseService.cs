using ChatApp_Server.DTOs;
using ChatApp_Server.Repositories;
using FluentResults;
using Mapster;
using Microsoft.AspNetCore.JsonPatch;

namespace ChatApp_Server.Services
{
    public interface IBaseService<TRepo, TEntity, TParam, TId, TDto>
       where TDto : class
       where TId: notnull
       where TRepo : class, IBaseRepository<TEntity, TId>
       where TEntity : class
    {
        Task<IEnumerable<TDto>> GetAllAsync(TParam stockEntryParameter);
        Task<TDto?> GetByIdAsync(TId id);
        Task<Result<TDto>> InsertAsync(TDto category);
        Task<Result> UpdateAsync(TId id, JsonPatchDocument<TDto> patchDoc);
        Task<Result> DeleteAsync(TId id);
    }
    public abstract class BaseService<TRepo, TEntity, TParam, TId, TDto>: IBaseService<TRepo, TEntity, TParam, TId, TDto>
        where TDto : class
        where TId : notnull
        where TRepo : class, IBaseRepository<TEntity, TId>
        where TEntity : class
    {
        protected readonly TRepo _repo;

        protected BaseService(TRepo repo)
        {
            _repo = repo;
        }
        public abstract Task<IEnumerable<TDto>> GetAllAsync(TParam parameter);
       
        public async Task<TDto?> GetByIdAsync(TId id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity.Adapt<TDto>();
        }

        public virtual async Task<Result<TDto>> InsertAsync(TDto dto)
        {
            var entity = dto.Adapt<TEntity>();
            _repo.Insert(entity);
            try
            {
                await _repo.SaveAsync();
                return Result.Ok(entity.Adapt<TDto>());
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

        public virtual async Task<Result> UpdateAsync(TId id, JsonPatchDocument<TDto> patchDoc)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
            {
                return Result.Fail("Bản ghi không tồn tại");
            }
            try
            {
                var newPatch = patchDoc.Adapt<JsonPatchDocument<TEntity>>();
                newPatch.ApplyTo(entity);
                _repo.Update(entity);
                await _repo.SaveAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }
        public virtual async Task<Result> DeleteAsync(TId id)
        {
            _repo.Delete(id);
            try
            {
                await _repo.SaveAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }

    }
}
