using System.ComponentModel.DataAnnotations;

namespace ServerWebAPI.Models
{
    public class ForgotPassword
    {
        [Required]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ!")]
        public string Email { get; set; }

        [Required]
        public string OTP { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự!")]
        public string NewPassword { get; set; }
    }
}
