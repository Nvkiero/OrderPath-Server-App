using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Models
{
    [Table("Shippers")]
    public class Shipper
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; } // FK UNIQUE

        [StringLength(50)]
        public string? Vehicle { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Available";

        // Thêm Rating vào đây
        // Dùng double để lưu số lẻ (ví dụ 4.5 sao)
        public double Rating { get; set; } = 5.0;

        [ForeignKey("UserId")]
        public User? UserInfo { get; set; }
    }
}