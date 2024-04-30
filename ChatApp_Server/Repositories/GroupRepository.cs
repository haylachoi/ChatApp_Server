using ChatApp_Server.Models;
using ChatApp_Server.Repositories;

namespace ChatApp_Server.Repositories
{
    public interface IGroupRepository: IBaseRepository<Group, int>
    {
        
    }
    public class GroupRepository : BaseRepository<Group, int>, IGroupRepository
    {
        public GroupRepository(ChatAppContext context) : base(context)
        {
        }

        public override void Delete(int id)
        {
            _context.Remove(new Group { Id = id });
        }
    }
}
