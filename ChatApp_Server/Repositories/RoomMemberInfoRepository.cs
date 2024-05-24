using ChatApp_Server.Criteria;
using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IRoomMemberInfoRepository: IBaseRepository<RoomMemberInfo, int>
    {
        Task<RoomMemberInfo?> GetAsync(MemberCriteria criteria);
        Task<IEnumerable<RoomMemberInfo>> GetAllAsync(MembersCriteria criteria);
        Task<RoomMemberInfo> UpdateFirstUnseenMessage(long messageId, int userId);
    }
    public class RoomMemberInfoRepository : BaseRepository<RoomMemberInfo, int>, IRoomMemberInfoRepository
    {
        public RoomMemberInfoRepository(ChatAppContext context) : base(context)
        {
        }

        public async Task<IEnumerable<RoomMemberInfo>> GetAllAsync(MembersCriteria criteria)
        {
            var query = _context.RoomMemberInfos.AsQueryable();
           
            if (criteria.RoomId != null)
                query = query.Where(m => m.RoomId == criteria.RoomId);

            if (criteria.UserId != null)
                query = query.Where(m => m.UserId == criteria.UserId);
                   
            if (criteria.IncludeUserInfo)
                query = query.Include(m => m.User);

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<RoomMemberInfo?> GetAsync(MemberCriteria criteria)
        {
            var query = _context.RoomMemberInfos.AsQueryable();
           
            if (criteria.Id != null)
                query = query.Where(m => m.Id == criteria.Id);
            else
            {
                if (criteria.RoomId != null)
                    query = query.Where(m => m.RoomId == criteria.RoomId);

                if (criteria.UserId != null)
                    query = query.Where(m => m.UserId == criteria.UserId);
            }

            if (criteria.HasOwner && criteria.OwnerId != null)
                query = query.Where(m => m.Room.GroupInfo!.GroupOwnerId == criteria.OwnerId);     

            if (criteria.IncludeUserInfo)
                query = query.Include(m => m.User);

            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<RoomMemberInfo> UpdateFirstUnseenMessage(long messageId, int userId)
        {
            var result = await _context.RoomMemberInfos.FromSqlInterpolated($"SELECT * FROM func_rm_update_first_unseen({messageId}, {userId})").FirstOrDefaultAsync();
            if (result == null)
            {
                throw new Exception("Something went wrong");
            }
            return result!;
        }
    }
}
