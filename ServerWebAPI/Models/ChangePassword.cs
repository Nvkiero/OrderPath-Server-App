using System.ComponentModel.DataAnnotations;

namespace ServerWebAPI.Models
{
    public class ChangePassword
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự!")]
        public string NewPassword { get; set; }
    }
}
