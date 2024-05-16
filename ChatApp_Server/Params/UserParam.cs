using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.Params
{
    public class UserParam
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email không được để trống")]
        public string Email { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password không được để trống")]
        public string Password { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Tên không được để trống")]
        public string Fullname { get; set; } = null!;

        public IFormFile? File { get; set; }
        public string? Avatar { get; set; }
    }
}
