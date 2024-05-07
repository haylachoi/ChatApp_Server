using ChatApp_Server.Models;

namespace ChatApp_Server.Repositories
{
    public interface IRoomMemberInfoRepository: IBaseRepository<RoomMemberInfo, int>
    {
    }
    public class RoomMemberInfoRepository : BaseRepository<RoomMemberInfo, int>, IRoomMemberInfoRepository
    {
        public RoomMemberInfoRepository(ChatAppContext context) : base(context)
        {
        }     
    }
}
