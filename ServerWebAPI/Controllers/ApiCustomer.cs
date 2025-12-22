using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using ServerWebAPI.Modules.Db_Orderpath; // Import Entity chuẩn

namespace ServerWebAPI.Controllers
{
    [Route("customer")]
    [ApiController]
    public class ApiCustomer : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiCustomer(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. PRODUCT (SẢN PHẨM)
        // ==========================================

        [HttpGet("product")]
        public async Task<IActionResult> GetListProduct()
        {
            // Lấy tất cả sản phẩm có Quantity > 0 (còn hàng)
            var products = await _context.Products
                .Where(p => p.Quantity > 0)
                .Select(p => new CustomerProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Quantity = p.Quantity ?? 0,
                    Image = p.Image,
                    Description = p.Description,
                    Category = p.Category
                })
                .ToListAsync();

            if (!products.Any()) return NotFound(new { message = "Không có sản phẩm nào" });

            return Ok(new { Products = products });
        }

        [HttpGet("product/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Sản phẩm không tồn tại" });

            return Ok(new CustomerProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Quantity = product.Quantity ?? 0,
                Image = product.Image,
                Description = product.Description,
                Category = product.Category
            });
        }

        [HttpGet("product/search")]
        public async Task<IActionResult> SearchProduct([FromQuery] string name)
        {
            if (string.IsNullOrEmpty(name)) return BadRequest("Vui lòng nhập tên");

            var products = await _context.Products
                .Where(p => p.Name!.ToLower().Contains(name.ToLower()))
                .Select(p => new CustomerProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Quantity = p.Quantity ?? 0,
                    Image = p.Image,
                    Category = p.Category
                })
                .ToListAsync();

            if (!products.Any()) return NotFound(new { message = "Không tìm thấy sản phẩm" });

            return Ok(new { SearchProducts = products });
        }

        // ==========================================
        // 2. CART (GIỎ HÀNG)
        // ==========================================

        [HttpGet("cart/{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .Select(c => new CartItemDTO
                {
                    ProductId = c.ProductId,
                    ProductName = c.Product!.Name,
                    Price = c.Product.Price,
                    Quantity = c.Quantity,
                    Image = c.Product.Image
                })
                .ToListAsync();

            if (!cartItems.Any()) return Ok(new { Items = new List<CartItemDTO>(), Message = "Giỏ hàng trống" });

            return Ok(new { Items = cartItems });
        }

        [HttpPost("cart/add")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartRequest req)
        {
            // Kiểm tra sản phẩm có tồn tại không
            var product = await _context.Products.FindAsync(req.ProductId);
            if (product == null) return NotFound("Sản phẩm không tồn tại");

            // Kiểm tra trong giỏ hàng đã có món này chưa
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == req.UserId && c.ProductId == req.ProductId);

            if (cartItem != null)
            {
                // Đã có -> Tăng số lượng
                cartItem.Quantity += req.Quantity;
            }
            else
            {
                // Chưa có -> Tạo mới
                cartItem = new CartItem
                {
                    UserId = req.UserId,
                    ProductId = req.ProductId,
                    Quantity = req.Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã thêm vào giỏ hàng" });
        }

        [HttpDelete("cart/remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId, [FromQuery] int userId)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem == null)
                return NotFound(new { success = false, message = "Sản phẩm không có trong giỏ hàng" });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa khỏi giỏ hàng" });
        }

        // ==========================================
        // 3. ORDER (ĐẶT HÀNG)
        // ==========================================

        [HttpPost("order/checkout")]
        public async Task<IActionResult> Checkout([FromQuery] int userId)
        {
            // 1. Lấy CartItem kèm thông tin Product (để lấy Giá và ShopId)
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest(new { success = false, message = "Giỏ hàng trống" });

            // 2. Tính tổng tiền
            decimal totalAmount = cartItems.Sum(c => (c.Product?.Price ?? 0) * c.Quantity);

            // 3. Tạo Order Master
            var order = new Order
            {
                UserId = userId,
                TotalAmount = totalAmount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                // ShipperId để null
            };

            // 4. Tạo List OrderItem
            // LƯU Ý QUAN TRỌNG: Phải gán ShopId cho OrderItem vì Database yêu cầu
            var orderItems = cartItems.Select(c => new OrderItem
            {
                ProductId = c.ProductId,
                Quantity = c.Quantity,
                Price = c.Product!.Price,
                ShopId = c.Product.ShopId // <--- Bắt buộc phải có cái này
            }).ToList();

            order.OrderItems = orderItems; // Gán list items vào order

            // 5. Lưu Order & Xóa Cart
            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems); // Xóa giỏ hàng sau khi mua

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                total = order.TotalAmount,
                message = "Đặt hàng thành công"
            });
        }

        [HttpGet("order/user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    OrderId = o.Id,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            if (!orders.Any())
                return NotFound(new { message = "Bạn chưa có đơn hàng nào" });

            return Ok(orders);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems) // Đổi Items -> OrderItems (theo Model chuẩn)
                .ThenInclude(oi => oi.Product) // Kèm thông tin tên sản phẩm
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var result = new
            {
                orderId = order.Id,
                createdAt = order.CreatedAt,
                total = order.TotalAmount,
                status = order.Status,
                items = order.OrderItems.Select(oi => new
                {
                    ProductName = oi.Product?.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    SubTotal = oi.Price * (oi.Quantity ?? 0)
                })
            };

            return Ok(result);
        }
    }

    // ==========================================
    // DTO CLASSES (Data Transfer Objects)
    // ==========================================

    public class CustomerProductDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Image { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
    }

    public class AddCartRequest
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CartItemDTO
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Image { get; set; }
    }
}