namespace ChatApp_Server.Criteria
{
    public class RoomCriteria
    {
        public int? Id { get; set; }
        public int? MemberId { get; set; }
        public int? OwnerId { get; set; }
        
        public bool IncludeMemberInfo { get; set; }

        public static RoomCriteria CreateWithAllInclude(int id)
            => new RoomCriteria { Id = id, IncludeMemberInfo = true };

    }
    public class RoomsCriteria: PagingCritera
    {      
        public int? MemberId { get; set; }      
        public bool IncludeMemberInfo { get; set; }       
    }

    public enum RoomSortBy
    {
        IdAsc,
        IdDesc
    }
}
