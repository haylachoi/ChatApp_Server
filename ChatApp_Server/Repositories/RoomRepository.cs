using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IRoomRepository : IBaseRepository<Room, int>
    {
        Task<Message?> GetLastUnseenMessage(int roomId, int userId);
        Task<Room?> GetOneWithInfoAsync(int roomId);
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

        public async Task<Room?> GetOneWithInfoAsync(int roomId)
        {
            return await GetOne(
                [r => r.Id == roomId], 
                query => query
                    .Include(r => r.GroupInfo).ThenInclude(gi => gi!.GroupOnwer)
                    .Include(r => r.RoomMemberInfos).ThenInclude(info => info.LastUnseenMessage)
                    .Include(r => r.RoomMemberInfos).ThenInclude(info => info.User));
        }
    }
}
