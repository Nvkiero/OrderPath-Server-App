using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // Để tránh lỗi loop khi trả về JSON

namespace ServerWebAPI.Modules.Db_Orderpath
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [JsonIgnore] // Không trả về mật khẩu khi query API
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Fullname { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }
    }
}