using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Models.Seller
{
    [Table("Orders")] // Tên bảng trong SQL
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        public int CustomerId { get; set; } // Giả sử lấy từ hệ thống User
        public decimal Total { get; set; }
        public string Status { get; set; } // "NEW", "CONFIRMED", "CANCELLED"
        public DateTime Date { get; set; } = DateTime.Now;
    }
}