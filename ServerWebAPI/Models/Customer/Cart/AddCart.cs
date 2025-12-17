namespace ServerWebAPI.Models.Customer.Cart
{
    public class AddCart
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } 
    }
}
