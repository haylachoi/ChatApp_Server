using ChatApp_Server.Criteria;
using ChatApp_Server.Models;
using ChatApp_Server.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IRefreshTokenRepository : IBaseRepository<RefreshToken, Guid>
    {
        Task<RefreshToken?> GetAsync(RefreshTokenCriteria criteria);
    }
    public class RefreshTokenRepository: BaseRepository<RefreshToken, Guid> ,IRefreshTokenRepository
    {

        public RefreshTokenRepository(ChatAppContext context) : base(context)
        {
        }

        public override void Delete(Guid id)
        {
            _context.RefreshTokens.Remove(new RefreshToken { Id = id });
        }

        public async Task<RefreshToken?> GetAsync(RefreshTokenCriteria criteria)
        {
            var query = _context.RefreshTokens.AsQueryable();

            if (criteria.Token != null)
                query = query.Where(tk => tk.Token == criteria.Token);

            return await query.AsNoTracking().FirstOrDefaultAsync();
        }
    }
}
