namespace ChatApp_Server.DTOs
{
    public class RoomDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public bool IsGroup { get; set; }
        public long? LastMessageId { get; set; }
        public long? FirstMessageId { get; set; }
        public RoomMemberInfo CurrentMemberInfo { get; set; } = null!;
        public IEnumerable<RoomMemberInfo> RoomMemberInfos { get; set; } = Enumerable.Empty<RoomMemberInfo>();
     
    }
    public class RoomMemberInfo
    {
        public int MemberId { get; set; }
        public string? FullName { get; set; }
        public long? FirstUnseenMessageId { get; set; }
        public long? LastUnseenMessageId { get; set; }
        public long UnseenMessageCount { get; set; }
        public Boolean CanDisplayRoom { get; set; }
        public Boolean CanShowNotification { get; set; }
        public PrivateMessageDto? LastUnseenMessage { get; set; }
        public UserDto? User { get; set; }
    }
}
