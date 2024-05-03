using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.Parameters
{
    public class ImageMessageParameter
    {
        [Required]
        public List<IFormFile> Files { get; set; } = null!;
        [Required]
        public int RoomId { get; set; }
    }
}
