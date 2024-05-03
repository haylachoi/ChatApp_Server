using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.Settings
{
    public class AppSettings
    {
        [Required]
        public required string SecretKey { get; set; }

        [Required]
        public required string GoogleApi { get; set; }

        [Required]
        public required string BucketName { get; set; }
    }
}
