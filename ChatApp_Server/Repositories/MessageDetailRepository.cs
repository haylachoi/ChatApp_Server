using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IMessageDetailRepository: IBaseRepository<MessageDetail, long>
    {
        Task<MessageDetail> AddOrUpdateReaction(long messageId, int roomId, int userId, int? reactionId);
        //Task<MessageDetail> AddOrUpdateIsReaded(long messageId, int userId);
    }
    public class MessageDetailRepository : BaseRepository<MessageDetail, long>, IMessageDetailRepository
    {
        public MessageDetailRepository(ChatAppContext context) : base(context)
        {
        }

        //public Task<MessageDetail> AddOrUpdateIsReaded(long messageId, int userId)
        //{
        //    var result = _context.MessageDetails.FromSqlInterpolated($"SELECT * FROM func_message_detail_update_is_readed({messageId}, {userId})").FirstOrDefaultAsync();
        //    if (result == null)
        //    {
        //        throw new Exception("Something went wrong");
        //    }
        //    return result!;
        //}

        public Task<MessageDetail> AddOrUpdateReaction(long messageId, int roomId, int userId, int? reactionId)
        {
            var result = _context.MessageDetails.FromSqlInterpolated($"SELECT * FROM func_message_detail_update_reaction({messageId}, {roomId}, {userId}, {reactionId})").FirstOrDefaultAsync();
            if (result == null)
            {
                throw new Exception("Something went wrong");               
            }
            return result!;
        }
    }
}
