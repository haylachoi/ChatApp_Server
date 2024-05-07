using ChatApp_Server.Models;
using Google.Api;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IMessageRepository:IBaseRepository<Message, long>
    {
        Task<IEnumerable<Message>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages);
        //Task<Message?> UpdateReactionMessage(long messageId, int receiverId, int? reactionId);
    }
    public class MessageRepository : BaseRepository<Message, long>, IMessageRepository
    {
        public MessageRepository(ChatAppContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Message>> GetSeenAndUnseenAsync(int roomId, long? firstUnseenMessageId, int numberMessages)
        {
            if (firstUnseenMessageId == null)
            {
                return _context.Messages
                           .Where(pm => pm.RoomId == roomId)
                           .Include(m => m.MessageDetails)
                           .OrderByDescending(pm => pm.CreatedAt).Take(numberMessages).OrderBy(pm => pm.CreatedAt);
            }
            var messages = await _context.Messages
                .Where(m => m.RoomId == roomId && m.Id <= firstUnseenMessageId)
                .Include(m => m.MessageDetails)
                .OrderByDescending(m => m.CreatedAt)
                .Take(numberMessages)
                .Union(_context.Messages
                    .Where(m => m.RoomId == roomId && m.Id > firstUnseenMessageId)
                    .Include(m => m.MessageDetails)
                    .OrderBy(pm => pm.CreatedAt)
                    .Take(numberMessages)).OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).ToListAsync();

            return messages;
         }

        //public async Task<Message?> UpdateReactionMessage(long messageId, int receiverId, int? reactionId)
        //{
        //    var result = await _context.Messages.FromSql($"SELECT * FROM func_pm_update_reaction({messageId},{receiverId} ,{reactionId})")
        //        .FirstOrDefaultAsync();

        //    return result;
        //}
    }
}
