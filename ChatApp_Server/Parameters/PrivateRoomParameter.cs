namespace ChatApp_Server.Parameters
{
    public class PrivateRoomParameter
    {

        public int? BiggerUserId { get; set; }

        public int? SmallerUserId { get; set; }

        public int? UserId { get; set; }

        public int NumberMessage { get; set; } = 15;
    }
}
