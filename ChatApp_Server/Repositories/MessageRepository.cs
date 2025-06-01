using ChatApp_Server.Criteria;
using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ChatApp_Server.Repositories
{
    public interface IMessageRepository:IBaseRepository<Message, long>
    {
        Task<Message?> GetAsync(MessageCriteria criteria);
        Task<IEnumerable<Message>> GetAllAsync(MessagesCriteria criteria);
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
                           .Include(m => m.Quote)
                           .OrderByDescending(pm => pm.Id).Take(numberMessages).OrderBy(pm => pm.Id);
            }
            var messages = await _context.Messages
                .Where(m => m.RoomId == roomId && m.Id <= firstUnseenMessageId)
                .Include(m => m.MessageDetails)
                .Include(m => m.Quote)
                .OrderByDescending(m => m.Id)
                .Take(numberMessages)
                .Union(_context.Messages
                    .Where(m => m.RoomId == roomId && m.Id > firstUnseenMessageId)
                    .Include(m => m.MessageDetails)
                    .Include(m => m.Quote)
                    .OrderBy(pm => pm.Id)
                    .Take(numberMessages)).OrderBy(m => m.Id).ThenBy(m => m.Id).ToListAsync();

            return messages;
         }

        public async Task<IEnumerable<Message>> GetPreviousMessagesAsync(int roomId, long messageId, int numberMessages)
        {
            var query = _context.Messages.Where(m => m.RoomId == roomId && m.Id < messageId).Include(m => m.MessageDetails).Include(m => m.Quote).OrderByDescending(m => m.Id).Take(numberMessages);
            var messages = await query.ToListAsync();

            return messages.OrderBy(m => m.Id);
        }
        public async Task<IEnumerable<Message>> GetNextMessagesAsync(int roomId, long messageId, int numberMessages)
        {
            var query = _context.Messages.Where(m => m.RoomId == roomId && m.Id > messageId).Include(m => m.MessageDetails).Include(m => m.Quote).OrderBy(m => m.Id).Take(numberMessages);

            return await query.ToListAsync();
        }

        public async Task<Message?> GetAsync(MessageCriteria criteria)
        {
            var query = _context.Messages.AsQueryable();
            if (criteria.Id != null)
            {
                query = query.Where(m => m.Id == criteria.Id);
            }

            query = query.Include(m => m.MessageDetails).Include(m => m.Quote);
            // EF build wrong query (inluce first unseen message id)
            //if (criteria.IsUserInRoom && criteria.UserId != null)
            //{
            //    query = query.Where(m => m.RoomMemberInfos.Any(m => m.UserId == criteria.UserId));
            //}

            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Message>> GetAllAsync(MessagesCriteria criteria)
        {
            var query = _context.Messages.AsQueryable();

            if (criteria.RoomId != null)
                query = query.Where(m => m.RoomId == criteria.RoomId);

            if (criteria.From != null)
                query = query.Where(m => m.Id >= criteria.From);

            if (criteria.To != null)
                query = query.Where(m => m.Id < criteria.To);

            query = query.OrderBy(m => m.Id);
            return await query.AsNoTracking().ToListAsync();
        }
    }
}
