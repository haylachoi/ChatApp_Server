using FirebaseAdmin.Messaging;
using System.ComponentModel.DataAnnotations;

namespace ChatApp_Server.Parameters
{
    public class RegisterParameter
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email không được để trống")]
        public string Email { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password không được để trống")]
        public string Password { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Tên không được để trống")]
        public string Fullname { get; set; } = null!;


    }
}
