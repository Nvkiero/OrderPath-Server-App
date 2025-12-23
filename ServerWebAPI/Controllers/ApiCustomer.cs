using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models; // Import Entity chuẩn

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
        // 1. PRODUCT (SẢN PHẨM)
        // lay danh sach san pham con hang
        [HttpGet("products")]
        public async Task<IActionResult> GetListProduct()
        {
            var products = await _context.Products
                .Where(p => (p.Quantity ?? 0) > 0)
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

            return Ok(products);
        }
        // lay chi tiet san pham theo id
        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Where(p => p.Id == id && (p.Quantity ?? 0) > 0)
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
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound(new { message = "Sản phẩm không tồn tại hoặc đã hết hàng" });

            return Ok(product);
        }
        // tim kiem danh sach san pham theo ten
        [HttpGet("products/search")]
        public async Task<IActionResult> SearchProduct([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Vui lòng nhập tên" });

            var products = await _context.Products
                .Where(p =>
                    (p.Quantity ?? 0) > 0 &&
                    EF.Functions.Like(p.Name!, $"%{name}%")
                )
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

            return Ok(products);
        }
        // 2. CART (GIỎ HÀNG)
        // lay danh sach san pham trong gio hang cua user da dang nhap
        [Authorize(Roles = "Customer,Seller")]
        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            int userId = int.Parse(User.FindFirst("userId")!.Value);

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId && c.Product != null)
                .Select(c => new CartItemDTO
                {
                    ProductId = c.ProductId,
                    ProductName = c.Product!.Name,
                    Price = c.Product.Price,
                    Quantity = c.Quantity,
                    Image = c.Product.Image
                })
                .ToListAsync();

            return Ok(cartItems);
        }

        [Authorize(Roles = "Customer,Seller")]
        [HttpPost("cart/add")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartRequest req)
        {
            if (req.Quantity <= 0)
                return BadRequest(new { message = "Số lượng không hợp lệ" });

            int userId = int.Parse(User.FindFirst("userId")!.Value);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == req.ProductId && (p.Quantity ?? 0) > 0);

            if (product == null)
                return NotFound(new { message = "Sản phẩm không tồn tại hoặc hết hàng" });

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == req.ProductId);

            int currentQty = cartItem?.Quantity ?? 0;
            if (currentQty + req.Quantity > product.Quantity)
                return BadRequest(new { message = "Vượt quá số lượng tồn kho" });

            if (cartItem != null)
            {
                cartItem.Quantity += req.Quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = req.ProductId,
                    Quantity = req.Quantity
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã thêm vào giỏ hàng" });
        }


        [Authorize(Roles = "Customer,Seller")]
        [HttpDelete("cart/remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            if (productId <= 0)
                return BadRequest(new { message = "ProductId không hợp lệ" });

            int userId = int.Parse(User.FindFirst("userId")!.Value);

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem == null)
                return NotFound(new { message = "Sản phẩm không có trong giỏ hàng" });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa khỏi giỏ hàng" });
        }

        // 3. ORDER (ĐẶT HÀNG)
        // checkout gio hang
        [Authorize(Roles = "Customer,Seller")]
        [HttpPost("order/checkout")]
        public async Task<IActionResult> Checkout()
        {
            int userId = int.Parse(User.FindFirst("userId")!.Value);
            // kiem tra gio hang co san pham chua de dat hang
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId && c.Product != null)
                .Select(c => new
                {
                    Cart = c,
                    Product = c.Product!
                })
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest(new { message = "Giỏ hàng trống" });

            // xem con hang khong
            foreach (var item in cartItems)
            {
                if (item.Cart.Quantity > item.Product.Quantity)
                    return BadRequest(new
                    {
                        message = $"Sản phẩm {item.Product.Name} không đủ tồn kho"
                    });
            }

            decimal totalAmount = cartItems.Sum(i => i.Product.Price * i.Cart.Quantity);

            var order = new Order
            {
                UserId = userId,
                TotalAmount = totalAmount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                OrderItems = cartItems.Select(i => new OrderItem
                {
                    ProductId = i.Product.Id,
                    Quantity = i.Cart.Quantity,
                    Price = i.Product.Price,
                    ShopId = i.Product.ShopId
                }).ToList()
            };

            // sau khi dat thanh cong thi giam so luong san pham trong kho
            foreach (var item in cartItems)
                item.Product.Quantity -= item.Cart.Quantity;

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems.Select(i => i.Cart));

            await _context.SaveChangesAsync();

            return Ok(new
            {
                orderId = order.Id,
                total = order.TotalAmount,
                message = "Đặt hàng thành công"
            });
        }
        // lay danh sach don hang cua user da dang nhap va da dat hang
        [Authorize(Roles = "Customer,Seller")]
        [HttpGet("order/my")]
        public async Task<IActionResult> GetMyOrders()
        {
            int userId = int.Parse(User.FindFirst("userId")!.Value);

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

            return Ok(orders);
        }

        // lay chi tiet tung don hang theo id
        [Authorize(Roles = "Customer,Seller")]
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            int userId = int.Parse(User.FindFirst("userId")!.Value);

            var order = await _context.Orders
                .Where(o => o.Id == orderId && o.UserId == userId)
                .Select(o => new
                {
                    orderId = o.Id,
                    createdAt = o.CreatedAt,
                    total = o.TotalAmount,
                    status = o.Status,
                    items = o.OrderItems.Select(oi => new
                    {
                        ProductName = oi.Product!.Name,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        SubTotal = oi.Price * oi.Quantity
                    })
                })
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound(new { message = "Đơn hàng không tồn tại" });

            return Ok(order);
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