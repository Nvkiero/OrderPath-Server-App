namespace ServerWebAPI.Models.Customer.Order
{
    public class GetDetailOrder
    {
        public class OrderResponse
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public double TotalAmount { get; set; }
            public string Status { get; set; } = "";
            public int? ShipperId { get; set; }
        }

    }
}
