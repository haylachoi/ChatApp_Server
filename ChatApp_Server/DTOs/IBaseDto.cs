namespace ChatApp_Server.DTOs
{
    public interface IBaseDto<TId>
    {
        TId Id { get; set; }
    }
}
