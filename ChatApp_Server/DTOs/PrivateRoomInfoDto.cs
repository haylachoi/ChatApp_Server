using ChatApp_Server.Models;

namespace ChatApp_Server.DTOs
{
    public class PrivateRoomInfoDto
    {
        public int? Id { get; set; }

        public int UserId { get; set; }

        public int PrivateRoomId { get; set; }

        public long? FirstUnseenMessageId { get; set; }
        public long? LastUnseenMessageId { get; set; }

        public long UnseenMessageCount { get; set; }
        public UserDto? User { get; set; }
    }
}
