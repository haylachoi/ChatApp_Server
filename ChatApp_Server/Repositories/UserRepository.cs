using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChatApp_Server.Repositories
{
    public interface IUserRepository : IBaseRepository<User, int>
    {
    }
    public class UserRepository : BaseRepository<User, int>, IUserRepository
    {  
        public UserRepository(ChatAppContext context) : base(context)
        {            
        }

        public override void Delete(int id)
        {
            _context.Users.Remove(new User { Id = id });
        }
    }
}
