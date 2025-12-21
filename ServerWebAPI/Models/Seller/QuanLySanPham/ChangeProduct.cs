namespace ServerWebAPI.Models.Seller
{
    public class ChangeProduct
    {
        public int id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; } 
        public string Description { get; set; }
        public string Category { get; set; }
    }
}
