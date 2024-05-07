using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IRoomRepository : IBaseRepository<Room, int>
    {
        Task<Message?> GetLastUnseenMessage(int roomId, int userId);
    }
    public class RoomRepository : BaseRepository<Room, int>, IRoomRepository
    {
        public RoomRepository(ChatAppContext context) : base(context)
        {
        }
        public async Task<Message?> GetLastUnseenMessage(int roomId, int userId)
        {
            var query = (from pr in _context.Rooms
                     join pr_info in _context.RoomMemberInfos
                     on new { room_id = pr.Id } equals new { room_id = pr_info.RoomId }
                     join pm in _context.Messages
                     on pr_info.LastUnseenMessageId equals pm.Id
                     where pr_info.LastUnseenMessageId == pr.Id && pr_info.UserId == userId && pr.Id == roomId                   
                     select pm);


            return await query.FirstOrDefaultAsync();
        }
    }
}
