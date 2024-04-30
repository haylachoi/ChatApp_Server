using ChatApp_Server.Models;

namespace ChatApp_Server.Repositories
{
    public interface IEmotionRepository: IBaseRepository<Emotion, int>
    {
    }
    public class EmotionRepository : BaseRepository<Emotion, int>, IEmotionRepository
    {
        public EmotionRepository(ChatAppContext context) : base(context)
        {
        }
    }
}
