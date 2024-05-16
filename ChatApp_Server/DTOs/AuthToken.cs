namespace ChatApp_Server.DTOs
{
    public class AuthToken
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }
}
