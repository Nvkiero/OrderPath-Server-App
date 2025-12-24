using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Models
{
    [Table("RefreshTokens")]
    public class RefreshToken // Refresh access token (dung sau)    
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiredAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
