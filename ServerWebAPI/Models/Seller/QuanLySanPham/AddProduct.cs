namespace ServerWebAPI.Models.Seller
{
    public class AddProduct
    {
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; } = "";
        public string Describtion { get; set; } = "";
        public string Category { get; set; } = "";
    }
}
