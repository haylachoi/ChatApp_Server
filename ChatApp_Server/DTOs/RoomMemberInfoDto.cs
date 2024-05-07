using ChatApp_Server.Models;

namespace ChatApp_Server.DTOs
{
    public class RoomMemberInfoDto
    {
        public int? Id { get; set; }

        public int UserId { get; set; }

        public int RoomId { get; set; }

        public long? FirstUnseenMessageId { get; set; }
        public long? LastUnseenMessageId { get; set; }
        public long UnseenMessageCount { get; set; }
        public bool CanDisplayRoom { get; set; }
        public bool CanShowNotification { get; set; }
        public MessageDto? LastUnseenMessage { get; set; }
        public UserDto? User { get; set; }
    }
}
