//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using ServerWebAPI.DataBase;
//using ServerWebAPI.Models;

//namespace ServerWebAPI.Controllers
//{
//    [ApiController]
//    [Route("seller")]
//    public class ApiSellerController : ControllerBase
//    {
//        private readonly AppDbContext _context;

//        public ApiSellerController(AppDbContext context)
//        {
//            _context = context;
//        }

//        // Helper: Lấy ShopId từ UserId
//        private async Task<Shop?> GetShopByUserId(int userId)
//        {
//            return await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
//        }

//        // ============================================
//        // 1. QUẢN LÝ SẢN PHẨM
//        // ============================================

//        // POST: /seller/products?userId=1
//        [HttpPost("products")]
//        public async Task<IActionResult> AddProduct([FromQuery] int userId, [FromBody] AddProductDTO dto)
//        {
//            var shop = await GetShopByUserId(userId);
//            if (shop == null) return BadRequest(new { message = "Bạn chưa đăng ký Shop" });

//            // Map DTO -> Entity Product
//            var newProduct = new Product
//            {
//                ShopId = shop.Id,
//                Name = dto.Name,
//                Price = dto.Price,
//                Quantity = dto.Quantity,
//                Image = dto.Image,
//                Category = dto.Category,
//                Description = dto.Description
//                // Lưu ý: Database SQL không có cột Status, nên không set Status ở đây
//            };

//            _context.Products.Add(newProduct);
//            await _context.SaveChangesAsync();

//            return Ok(new { ProductId = newProduct.Id, Status = "Success", Message = "Thêm thành công" });
//        }

//        // PUT: /seller/products/{id}?userId=1
//        [HttpPut("products/{id}")]
//        public async Task<IActionResult> UpdateProduct(int id, [FromQuery] int userId, [FromBody] ChangeProductDTO dto)
//        {
//            // Kiểm tra ID url và ID body có khớp không
//            if (id != dto.id) return BadRequest("ID sản phẩm không khớp");

//            var shop = await GetShopByUserId(userId);
//            if (shop == null) return Unauthorized();

//            // Tìm sản phẩm thuộc shop này
//            var product = await _context.Products
//                .FirstOrDefaultAsync(p => p.Id == id && p.ShopId == shop.Id);

//            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

//            // Cập nhật dữ liệu
//            product.Name = dto.Name;
//            product.Price = dto.Price;
//            product.Quantity = dto.Quantity;
//            product.Image = dto.Image;
//            product.Category = dto.Category;
//            product.Description = dto.Description;

//            await _context.SaveChangesAsync();
//            return Ok(new { Status = "Success", Message = "Cập nhật thành công" });
//        }

//        // DELETE: /seller/products/{id}?userId=1
//        [HttpDelete("products/{id}")]
//        public async Task<IActionResult> DeleteProduct(int id, [FromQuery] int userId)
//        {
//            var shop = await GetShopByUserId(userId);
//            if (shop == null) return Unauthorized();

//            var product = await _context.Products
//                .FirstOrDefaultAsync(p => p.Id == id && p.ShopId == shop.Id);

//            if (product == null) return NotFound();

//            // Xóa cứng khỏi DB (vì bảng Products không có cột IsDeleted)
//            _context.Products.Remove(product);
//            await _context.SaveChangesAsync();

//            return Ok(new { Status = "Success", Message = "Đã xóa sản phẩm" });
//        }

//        // GET: /seller/products?userId=1
//        [HttpGet("products")]
//        public async Task<IActionResult> GetProductList([FromQuery] int userId)
//        {
//            var shop = await GetShopByUserId(userId);
//            if (shop == null) return BadRequest("Chưa có Shop");

//            // Lấy list entity -> Map sang ProductResponseDTO
//            var products = await _context.Products
//                .Where(p => p.ShopId == shop.Id)
//                .Select(p => new ProductResponseDTO
//                {
//                    Id = p.Id,
//                    Name = p.Name ?? "",
//                    Price = p.Price,
//                    Quantity = p.Quantity ?? 0,
//                    Image = p.Image ?? "",
//                    // SQL không có cột Status, ta giả định còn hàng (>0) là true
//                    Status = (p.Quantity > 0)
//                })
//                .ToListAsync();

//            return Ok(products);
//        }

//        // ============================================
//        // 2. QUẢN LÝ ĐƠN HÀNG
//        // ============================================

//        // GET: /seller/orders?userId=1
//        [HttpGet("orders")]
//        public async Task<IActionResult> GetOrders([FromQuery] int userId)
//        {
//            var shop = await GetShopByUserId(userId);
//            if (shop == null) return Unauthorized();

//            // Logic: Lấy các OrderItem thuộc Shop này, gom nhóm theo OrderId
//            // để tính tổng tiền mà Shop nhận được từ đơn đó (chứ không phải tổng tiền cả đơn nếu khách mua nhiều shop)

//            var orders = await _context.OrderItems
//                .Where(oi => oi.ShopId == shop.Id)
//                .Include(oi => oi.Order)
//                .GroupBy(oi => oi.Order) // Group theo đơn hàng cha
//                .Select(g => new OrderResponseDTO
//                {
//                    OrderId = g.Key!.Id,
//                    CustomerId = g.Key.UserId,
//                    // Tổng tiền = Tổng (Giá * Số lượng) của các món thuộc Shop này
//                    Total = g.Sum(oi => oi.Price * (oi.Quantity ?? 0)),
//                    Status = g.Key.Status ?? "Pending",
//                    Date = g.Key.CreatedAt
//                })
//                .OrderByDescending(o => o.Date)
//                .ToListAsync();

//            return Ok(orders);
//        }

//        // PUT: /seller/orders/{id}/confirm
//        [HttpPut("orders/{id}/confirm")]
//        public async Task<IActionResult> ConfirmOrder(int id)
//        {
//            // Tìm đơn hàng
//            var order = await _context.Orders.FindAsync(id);
//            if (order == null) return NotFound();

//            // Cập nhật trạng thái
//            order.Status = "Shipping"; // Hoặc "Confirmed" tùy quy ước
//            await _context.SaveChangesAsync();

//            return Ok(new { Status = "Confirmed", Message = "Đơn hàng đã được xác nhận" });
//        }

//        // PUT: /seller/orders/{id}/cancel
//        [HttpPut("orders/{id}/cancel")]
//        public async Task<IActionResult> CancelOrder(int id)
//        {
//            var order = await _context.Orders.FindAsync(id);
//            if (order == null) return NotFound();

//            order.Status = "Cancelled";
//            await _context.SaveChangesAsync();

//            return Ok(new { Status = "Cancelled", Message = "Đơn hàng đã hủy" });
//        }
//    }
//}