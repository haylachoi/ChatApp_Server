using ChatApp_Server.Criteria;
using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IMessageDetailRepository: IBaseRepository<MessageDetail, long>
    {
        Task<MessageDetail> AddOrUpdateReaction(long messageId, int roomId, int userId, int reactionId);
        Task<MessageDetail?> GetAsync(MessageDetailCriteria criteria);
    }
    public class MessageDetailRepository : BaseRepository<MessageDetail, long>, IMessageDetailRepository
    {
        public MessageDetailRepository(ChatAppContext context) : base(context)
        {
        }

        public Task<MessageDetail> AddOrUpdateReaction(long messageId, int roomId, int userId, int reactionId)
        {
            var result = _context.MessageDetails.FromSqlInterpolated($"SELECT * FROM func_message_detail_update_reaction({messageId}, {roomId}, {userId}, {reactionId})").FirstOrDefaultAsync();
            if (result == null)
            {
                throw new Exception("Something went wrong");               
            }
            return result!;
        }

        public async Task<MessageDetail?> GetAsync(MessageDetailCriteria criteria)
        {
            var query = _context.MessageDetails.AsQueryable();

            if (criteria.UserId != null)
                query = query.Where(md => md.UserId == criteria.UserId);

            if (criteria.MessageId != null)
                query = query.Where(md => md.MessageId == criteria.MessageId);

            return await query.AsNoTracking().FirstOrDefaultAsync();
        }
    }
}
