using System.ComponentModel.DataAnnotations;

namespace ServerWebAPI.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public double Price { get; set; }

        public int Quantity { get; set; }

        public string Image { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }
}