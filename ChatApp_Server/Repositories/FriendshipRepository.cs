using ChatApp_Server.Models;
using ChatApp_Server.Repositories;

namespace ChatApp_Server.Repositories
{
    public interface IFriendshipRepository: IBaseRepository<Friendship, int>
    {

    }
    public class FriendshipRepository : BaseRepository<Friendship, int>, IFriendshipRepository
    {
        public FriendshipRepository(ChatAppContext context) : base(context)
        {
        }

        public override void Delete(int id)
        {
            _context.Friendships.Remove(new Friendship { Id = id });
        }
    }
}
