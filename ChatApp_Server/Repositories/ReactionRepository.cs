using ChatApp_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp_Server.Repositories
{
    public interface IReactionRepository
    {
        Task<IEnumerable<Reaction>> GetAllAsync();
    }
    public class ReactionRepository(ChatAppContext _context) : IReactionRepository
    {
        public async Task<IEnumerable<Reaction>> GetAllAsync()
        {
            return await _context.Reactions.OrderBy(r => r.Id).AsNoTracking().ToListAsync();
        }
    }
}
