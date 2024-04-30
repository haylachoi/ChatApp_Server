namespace ChatApp_Server.DTOs
{
    public class BlocklistDto: IBaseDto<int?>
    {
        public int? Id { get; set; }

        public int BlockerId { get; set; }

        public int BlockedId { get; set; }
    }
}
