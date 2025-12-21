namespace ServerWebAPI.Models.Seller
{
    // Hứng dữ liệu khi Client thêm mới
    public class AddProductDTO
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
    }

    // Hứng dữ liệu khi Client sửa
    public class ChangeProductDTO : AddProductDTO
    {
        public int id { get; set; }
    }

    // Trả về dữ liệu cho Client hiển thị Grid
    public class ProductResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
        public bool Status { get; set; }
    }

    // Trả về dữ liệu đơn hàng
    public class OrderResponseDTO
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }
}