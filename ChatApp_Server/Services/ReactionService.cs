using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using Mapster;

namespace ChatApp_Server.Services
{
    public interface IReactionService: IBaseService<IReactionRepository, Reaction, ReactionParameter, int, ReactionDto>
    {
    }
    public class ReactionService : BaseService<IReactionRepository, Reaction, ReactionParameter, int, ReactionDto>, IReactionService
    {
        public ReactionService(IReactionRepository repo) : base(repo)
        {
        }

        public override async Task<IEnumerable<ReactionDto>> GetAllAsync(ReactionParameter parameter)
        {
            var reactions = await _repo.GetAllAsync();
            return reactions.Adapt<IEnumerable<ReactionDto>>();
        }
    }
}
