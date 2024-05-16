namespace ChatApp_Server.DTOs
{
    public class ProfileDto
    {
        public int? Id { get; set; }
        public string Fullname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Avatar { get; set; }
        public bool IsOnline { get; set; }
    }
}
