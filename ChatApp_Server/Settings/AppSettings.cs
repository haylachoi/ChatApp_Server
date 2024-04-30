using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.Settings
{
    public class AppSettings
    {
        [Required]
        public required string SecretKey { get; set; }
    }
}
