namespace ChatApp_Server.DTOs
{
    public class RoomDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsGroup { get; set; }
        public string? Avatar {  get; set; }
        public long? LastMessageId { get; set; }
        public long? FirstMessageId { get; set; }
        public RoomMemberInfoDto CurrentRoomInfo { get; set; } = null!;
        public IEnumerable<RoomMemberInfoDto> RoomMemberInfos { get; set; } = Enumerable.Empty<RoomMemberInfoDto>();
     
    }
  
}
