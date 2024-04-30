using ChatApp_Server.DTOs;
using ChatApp_Server.Models;
using ChatApp_Server.Parameters;
using ChatApp_Server.Repositories;
using Mapster;

namespace ChatApp_Server.Services
{
    public interface IEmotionService: IBaseService<IEmotionRepository, Emotion, EmotionParameter, int, EmotionDto>
    {
    }
    public class EmotionService : BaseService<IEmotionRepository, Emotion, EmotionParameter, int, EmotionDto>, IEmotionService
    {
        public EmotionService(IEmotionRepository repo) : base(repo)
        {
        }

        public override async Task<IEnumerable<EmotionDto>> GetAllAsync(EmotionParameter parameter)
        {
            var emotions = await _repo.GetAllAsync();
            return emotions.Adapt<IEnumerable<EmotionDto>>();
        }
    }
}
