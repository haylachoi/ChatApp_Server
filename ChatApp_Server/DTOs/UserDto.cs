using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp_Server.DTOs
{
    public class UserDto: IBaseDto<int?>
    {
        public int? Id { get; set; }     
        public string Fullname { get; set; } = null!;

        [JsonIgnore]
        public string Email { get; set; } = null!;
        public string? Avatar { get; set; }
        public bool IsOnline { get; set; }     
    }
}
