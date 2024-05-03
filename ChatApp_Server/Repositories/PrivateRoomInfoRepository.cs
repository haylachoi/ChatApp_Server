using ChatApp_Server.Models;

namespace ChatApp_Server.Repositories
{
    public interface IPrivateRoomInfoRepository: IBaseRepository<PrivateRoomInfo, int>
    {
    }
    public class PrivateRoomInfoRepository : BaseRepository<PrivateRoomInfo, int>, IPrivateRoomInfoRepository
    {
        public PrivateRoomInfoRepository(ChatAppContext context) : base(context)
        {
        }
    }
}
