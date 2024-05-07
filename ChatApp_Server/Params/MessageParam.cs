namespace ChatApp_Server.Params
{
    public class MessageParam
    {
        public string Content { get; set; } = null!;
        public bool IsImage { get; set; }
        public int SenderId { get; set; }
        public int RoomId { get; set; }
    }
}
