using ChatApp_Server.Models;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChatApp_Server.Repositories
{
    public interface IUserRepository : IBaseRepository<User, int>
    {
        Task<IEnumerable<User?>> SearchUser(string searchTerm, int userId);
    }
    public class UserRepository : BaseRepository<User, int>, IUserRepository
    {  
        public UserRepository(ChatAppContext context) : base(context)
        {            
        }

        public async Task<IEnumerable<User?>> SearchUser(string searchTerm, int userId)
        {

            var excludedUserIds = _context.Rooms
                 .Where(r => r.RoomMemberInfos.Any(rm => rm.UserId == userId) && !r.IsGroup)
                 .Select(r => r.Id);

            var query = _context.Users
                .Where(u => u.Fullname.Contains(searchTerm) && !_context.RoomMemberInfos
                    .Any(rm => rm.UserId == u.Id && excludedUserIds.Contains(rm.RoomId)));

            return await query.ToListAsync();
        }
        public override void Delete(int id)
        {
            _context.Users.Remove(new User { Id = id });
        }
    }
}
