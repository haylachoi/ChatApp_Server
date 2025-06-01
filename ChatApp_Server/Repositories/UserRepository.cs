using ChatApp_Server.Criteria;
using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChatApp_Server.Repositories
{
    public interface IUserRepository : IBaseRepository<User, int>
    {
        Task<User?> GetAsync(UserCriteria criteria);
        Task<IEnumerable<User>> SearchUsersNotInAllPrivateRooms(string searchTerm, int userId);
        Task<IEnumerable<User>> SearchUserNotInRoom(int roomId, string searchTerm);
    }
    public class UserRepository : BaseRepository<User, int>, IUserRepository
    {  
        public UserRepository(ChatAppContext context) : base(context)
        {            
        }

        // Search users not in private room
        public async Task<IEnumerable<User>> SearchUsersNotInAllPrivateRooms(string searchTerm, int userId)
        {
            // private rooms user is in
            var excludedUserIds = _context.Rooms
                 .Where(r => r.RoomMemberInfos.Any(rm => rm.UserId == userId) && !r.IsGroup)
                 .Select(r => r.Id);

            var query = _context.Users
                .Where(u => u.Fullname.Contains(searchTerm) && !_context.RoomMemberInfos
                    .Any(rm => rm.UserId == u.Id && excludedUserIds.Contains(rm.RoomId)));

            return await query.Take(10).AsNoTracking().ToListAsync();
        }
        public override void Delete(int id)
        {
            _context.Users.Remove(new User { Id = id });
        }

        public async Task<IEnumerable<User>> SearchUserNotInRoom(int roomId, string searchTerm)
        {
            var query = _context.Users.Where(u => u.Fullname.Contains(searchTerm) && !u.RoomMemberInfos.Any(info => info.RoomId == roomId));

            return await query.Take(10).AsNoTracking().ToListAsync();
        }

        public async Task<User?> GetAsync(UserCriteria criteria)
        {
            var query = _context.Users.AsQueryable();
            if (criteria.UserId != null)            
                query = query.Where(u => u.Id == criteria.UserId);
            
            if (criteria.Email != null)
                query = query.Where(u => u.Email == criteria.Email);

            return await query.AsNoTracking().FirstOrDefaultAsync();
        }
    }
}
