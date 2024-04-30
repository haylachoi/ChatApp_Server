using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IPrivateRoomRepository : IBaseRepository<PrivateRoom, int>
    {
        Task<PrivateMessage?> GetLastUnseenMessage(int roomId, int userId);
    }
    public class PrivateRoomRepository : BaseRepository<PrivateRoom, int>, IPrivateRoomRepository
    {
        public PrivateRoomRepository(ChatAppContext context) : base(context)
        {
        }
        public async Task<PrivateMessage?> GetLastUnseenMessage(int roomId, int userId)
        {
            var query = (from pr in _context.PrivateRooms
                     join pr_info in _context.PrivateRoomInfos
                     on new { room_id = pr.Id } equals new { room_id = pr_info.PrivateRoomId }
                     join pm in _context.PrivateMessages
                     on pr_info.LastUnseenMessageId equals pm.Id
                     where pr_info.LastUnseenMessageId == pr.Id && pr_info.UserId == userId && pr.Id == roomId                   
                     select pm);


            return await query.FirstOrDefaultAsync();
        }
    }
}
