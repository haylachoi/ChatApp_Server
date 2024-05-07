using ChatApp_Server.Models;

namespace ChatApp_Server.DTOs
{
    public class MessageDetailDto
    {
        public long? Id { get; set; }

        public long MessageId { get; set; }
        public int UserId { get; set; }

        public bool? IsReaded { get; set; }

        public int? ReactionId { get; set; }
        public ReactionDto? Reaction { get; set; }
        public UserDto User { get; set; } = null!;
        //public MessageDto Message { get; set; } = null!;
    }
}
