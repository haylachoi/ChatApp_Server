using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp_Server.DTOs
{
    public class UserDto: IBaseDto<int?>
    {
        public int? Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Fullname { get; set; } 
        public string? Password { get; set; } 

        public string? Avatar { get; set; }
        public bool IsOnline { get; set; }

        public bool ShouldSerializePassword()
        {
            return false;
        }
    }
}
