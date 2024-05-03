using ChatApp_Server.Models;

namespace ChatApp_Server.Repositories
{
    public interface IReactionRepository: IBaseRepository<Reaction, int>
    {
    }
    public class ReactionRepository : BaseRepository<Reaction, int>, IReactionRepository
    {
        public ReactionRepository(ChatAppContext context) : base(context)
        {
        }
    }
}
