namespace ChatApp_Server.Criteria
{
    public class MessageCriteria
    {
        public long? Id { get; set; }
        //public int? UserId { get; set; }
        //public bool IsUserInRoom { get; set; }
    }
    public class MessagesCriteria
    {
        public int? RoomId { get; set; }
        public long? From { get; set; }
        public long? To { get; set; }
    }
}
