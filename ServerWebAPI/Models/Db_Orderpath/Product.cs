using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerWebAPI.Modules.Db_Orderpath
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public int ShopId { get; set; }

        [StringLength(100)]
        public string? Name { get; set; }

        [Column(TypeName = "decimal(18,2)")] // Định dạng tiền tệ SQL
        public decimal Price { get; set; }

        public int? Quantity { get; set; }

        [StringLength(255)]
        public string? Image { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [ForeignKey("ShopId")]
        public Shop? Shop { get; set; }
    }
}