namespace ChatApp_Server.DTOs
{
    public class JwtTokenDto
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }
}
