namespace ChatApp_Server.Criteria
{
    public class MemberCriteria
    {
        public int? Id { get; set; }
        public int? RoomId { get; set; }
        public int? UserId { get; set; }
        public int? OwnerId { get; set;}
        public bool HasOwner { get; set; }
        public bool IncludeUserInfo { get; set; }

    }
    public class MembersCriteria
    {     
        public int? RoomId { get; set; }
        public int? UserId { get; set; }      
        public bool IncludeUserInfo { get; set; }
    }
}
