using ChatApp_Server.Models;
using ChatApp_Server.Repositories;

namespace ChatApp_Server.Repositories
{
    public interface IRefreshTokenRepository : IBaseRepository<RefreshToken, Guid>
    {
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
    }
}
