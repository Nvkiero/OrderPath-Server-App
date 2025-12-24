namespace ServerWebAPI.Models
{

    //Dto cho product
    // 1. Dùng khi Thêm sản phẩm mới
    public class AddProductDTO
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Image { get; set; }       // Link ảnh (có thể null)
        public string? Category { get; set; }    // Danh mục
        public string? Description { get; set; } // Mô tả
    }

    // 2. Dùng khi Sửa sản phẩm 
    public class ChangeProductDTO : AddProductDTO
    {
        public int Id { get; set; } // Cần thêm ID để biết đang sửa sản phẩm nào
    }

    // 3. Dùng để trả về dữ liệu hiển thị lên Web (Get All Products)
    public class ProductResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; } = string.Empty;
        public bool Status { get; set; } // True: Còn hàng/Đang bán
    }

    //DTO cho đơn hàng (order)
    // 4. Dùng để hiển thị danh sách đơn hàng cho Seller xem
    public class SellerOrderResponseDTO
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        // Đây là tổng tiền shop nhận được trong đơn đó 
        // (Ví dụ đơn 1 triệu, nhưng chỉ mua của shop mình 200k thì hiện 200k)
        public decimal TotalRevenue { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}