using System.ComponentModel.DataAnnotations;

namespace ServerWebAPI.Models
{
    public class ChangePasswordDTO
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
    