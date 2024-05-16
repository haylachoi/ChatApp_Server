using ChatApp_Server.DTOs;
using ChatApp_Server.Repositories;
using Mapster;

namespace ChatApp_Server.Services
{
    public interface IReactionService
    {
        Task<IEnumerable<ReactionDto>> GetAllAsync();
    }
    public class ReactionService(IReactionRepository _repo) : IReactionService
    {      
        public async Task<IEnumerable<ReactionDto>> GetAllAsync()
        {
            var reactions = await _repo.GetAllAsync();
            return reactions.Adapt<IEnumerable<ReactionDto>>();
        }
    }
}
