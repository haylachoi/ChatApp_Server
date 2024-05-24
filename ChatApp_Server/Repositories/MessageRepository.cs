using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ChatApp_Server.Repositories
{
    public interface IMessageRepository:IBaseRepository<Message, long>
    {
        Task<IEnumerable<Message>> GetSeenAndUnseenAsync(int roomId, int userId, int numberMessages);
        Task<IEnumerable<Message>> GetPreviousMessagesAsync(int roomId, long messageId, int numberMessages);
        Task<IEnumerable<Message>> GetNextMessagesAsync(int roomId, long messageId, int numberMessages);

    }
    public class MessageRepository : BaseRepository<Message, long>, IMessageRepository
    {
        public MessageRepository(ChatAppContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Message>> GetSeenAndUnseenAsync(int roomId,int userId, int numberMessages)
        {
            var member = 
                await _context.RoomMemberInfos.Where(m => m.RoomId == roomId && m.UserId == userId).FirstOrDefaultAsync() 
                ?? throw new Exception("Bạn không ở trong nhóm");

            var firstUnseenMessageId = member.FirstUnseenMessageId;
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

        public async Task<IEnumerable<Message>> GetPreviousMessagesAsync(int roomId, long messageId, int numberMessages)
        {
            var query = _context.Messages.Where(m => m.RoomId == roomId && m.Id < messageId).OrderByDescending(m => m.Id).Take(numberMessages);
            var messages = await query.ToListAsync();

            return messages.OrderBy(m => m.Id);
        }
        public async Task<IEnumerable<Message>> GetNextMessagesAsync(int roomId, long messageId, int numberMessages)
        {
            var query = _context.Messages.Where(m => m.RoomId == roomId && m.Id > messageId).OrderBy(m => m.Id).Take(numberMessages);

            return await query.ToListAsync();
        }
    }
}
