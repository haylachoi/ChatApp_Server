using ChatApp_Server.Models;
using Google.Api;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IPrivateMessageRepository:IBaseRepository<PrivateMessage, long>
    {
        Task<IEnumerable<PrivateMessage>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages);
        Task<PrivateMessage?> UpdateReactionMessage(long messageId, int receiverId, int? reactionId);
    }
    public class PrivateMessageRepository : BaseRepository<PrivateMessage, long>, IPrivateMessageRepository
    {
        public PrivateMessageRepository(ChatAppContext context) : base(context)
        {
        }
        public async Task<IEnumerable<PrivateMessage>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages)
        {
            if (firstUnseenMessageId == null)
            {
                return _context.PrivateMessages
                           .Where(pm => pm.PrivateRoomId == roomId)
                           .OrderByDescending(pm => pm.CreatedAt).Take(numberMessages).OrderBy(pm => pm.CreatedAt);
            }
            return await _context.PrivateMessages
                .Where(pm => pm.PrivateRoomId == roomId && pm.Id <= firstUnseenMessageId)
                .OrderByDescending(pm => pm.CreatedAt)
                .Take(numberMessages)
                .Union(_context.PrivateMessages
                    .Where(pm => pm.PrivateRoomId == roomId && pm.Id > firstUnseenMessageId)
                    .OrderBy(pm => pm.CreatedAt)
                    .Take(numberMessages)).OrderBy(pm => pm.CreatedAt).ThenBy(pm => pm.Id).ToListAsync();
         }

        public async Task<PrivateMessage?> UpdateReactionMessage(long messageId, int receiverId, int? reactionId)
        {
            var result = await _context.PrivateMessages.FromSql($"SELECT * FROM func_pm_update_reaction({messageId},{receiverId} ,{reactionId})")
                .FirstOrDefaultAsync();

            return result;
        }
    }
}
