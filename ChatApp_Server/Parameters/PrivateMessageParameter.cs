namespace ChatApp_Server.Parameters
{
    public class PrivateMessageParameter
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }

        public int PrivateRoomId { get; set; }
        public int NumberMessages { get; set; }
        public bool IsTwoWay { get; set; } = true;
    }
}
