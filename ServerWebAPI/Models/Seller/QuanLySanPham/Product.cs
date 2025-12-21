using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Models.Seller
{
    [Table("Products")] // Tên bảng trong SQL
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
        public bool Status { get; set; } = true; // True = Đang bán, False = Ẩn
        public string Category { get; set; }
        public string Description { get; set; }
    }
}