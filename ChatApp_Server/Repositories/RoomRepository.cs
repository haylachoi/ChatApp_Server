using ChatApp_Server.Criteria;
using ChatApp_Server.Models;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq;
using System.Linq.Expressions;

namespace ChatApp_Server.Repositories
{
    public interface IRoomRepository : IBaseRepository<Room, int>
    {             
        Task<Message?> GetFirstMessage(int roomId);
        Task<Message?> GetLastMessage(int roomId);  
        Task<Room?> GetAsync(RoomCriteria criteria);
        Task<IEnumerable<Room>> GetAllAsync(RoomsCriteria criteria);
    }
    public class RoomRepository : BaseRepository<Room, int>, IRoomRepository
    {
        public RoomRepository(ChatAppContext context) : base(context) { }
       
        public async Task<Room?> GetAsync(RoomCriteria criteria)
        {
            var query = _context.Rooms.AsQueryable();

            if (criteria.Id != null)
                query = query.Where(r => r.Id == criteria.Id);

            if (criteria.MemberId != null)
                query = query.Where(r => r.RoomMemberInfos.Any(m => m.UserId == criteria.MemberId));

            if (criteria.OwnerId != null)
                query = query.Where(r => r.GroupInfo!.GroupOwnerId == criteria.OwnerId);


            query = query.Include(r => r.GroupInfo).ThenInclude(gf => gf!.GroupOwner);
            query = query.Include(r => r.FirstMessage).Include(r => r.LastMessage);

            if (criteria.IncludeMemberInfo)
                query = query.Include(r => r.RoomMemberInfos).ThenInclude(m => m.User);
          
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Room>> GetAllAsync(RoomsCriteria criteria)
        {
            var query = _context.Rooms.AsQueryable();

            if (criteria.MemberId != null)
                query = query.Where(r => r.RoomMemberInfos.Any(m => m.UserId == criteria.MemberId));

            query = query.Include(r => r.GroupInfo).ThenInclude(gf => gf!.GroupOwner);
            query = query.Include(r => r.FirstMessage).Include(r => r.LastMessage);

            if (criteria.IncludeMemberInfo)
            {
                query = query.Include(r => r.RoomMemberInfos).ThenInclude(m => m.User);
            }

           
            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<Message?> GetFirstMessage(int roomId)
        {
            return await _context.Rooms.Where(r => r.Id == roomId).Select(r => r.FirstMessage).FirstOrDefaultAsync();
        }

        public async Task<Message?> GetLastMessage(int roomId)
        {
            return await _context.Rooms.Where(r => r.Id == roomId).Select(r => r.LastMessage).FirstOrDefaultAsync();

        }
    }
}
