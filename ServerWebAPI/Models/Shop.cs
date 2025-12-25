using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Models
{
    [Table("Shop")]
    public class Shop
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; } // FK UNIQUE

        [Required]
        [StringLength(100)]
        public string ShopName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? Owner { get; set; }
    }
}