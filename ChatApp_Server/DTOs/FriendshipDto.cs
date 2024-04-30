namespace ChatApp_Server.DTOs
{
    public class FriendshipDto: IBaseDto<int>
    {
        public int Id { get; set; }      

        public int SenderId { get; set; }

        public int ReceiverId { get; set; }

        public bool? IsAccepted { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
