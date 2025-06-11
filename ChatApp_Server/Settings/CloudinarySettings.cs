using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.Settings
{
    public class CloudinarySettings
    {
        [Required]
        public required string CloudName { get; set; }
        [Required]
        public required string ApiKey { get; set; }
        [Required]
        public required string ApiSecret { get; set; }
    }
}
