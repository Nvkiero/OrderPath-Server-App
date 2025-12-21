using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;
using ServerWebAPI.Models.Customer.Cart;
using ServerWebAPI.Models.Customer.Order;
using ServerWebAPI.Models.Customer.Product;
using System.ComponentModel.DataAnnotations;
using static ServerWebAPI.Models.Customer.Order.GetDetailOrder;

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

        [HttpGet("product")]
        public async Task<IActionResult> GetListProduct()
        {
            var ListProducts = await _context.Products.ToListAsync();
            if (!ListProducts.Any()) return NotFound();
            return Ok(new GetProductList
                {
                    Products = ListProducts
                });
        }

        [HttpGet("product/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return Ok(new ProductDetail
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Quantity = product.Quantity,
                Image = product.Image,
                Description = product.Description,
                Category = product.Category
            });
        }

        [HttpGet("product/search")]
        public async Task<IActionResult> SearchProduct([FromQuery] string name)
        {
            var products = await _context.Products
                .Where(p => p.Name.ToLower().Contains(name))
                .ToListAsync();
            if (products == null) return NotFound();
            return Ok(new SearchProduct
            {   
                SearchProducts = products
            });
        }

        [HttpGet("cart/{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return NotFound();

            return Ok(new GetCartResponse { Items = cartItems });
        }

        [HttpPost("cart/add")]
        public async Task<IActionResult> AddToCart([FromBody] AddCart req)
        {
            var product = await _context.Products.FindAsync(req.ProductId);
            if (product == null) return NotFound("Product not found");

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    c.UserId == req.UserId && c.ProductId == req.ProductId);

            if (cartItem != null)
            {
                cartItem.Quantity += req.Quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    UserId = req.UserId,
                    ProductId = req.ProductId,
                    Quantity = req.Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok("Added to cart");
        }

        [HttpDelete("cart/remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId, [FromQuery] int userId)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId && c.ProductId == productId);

            if (cartItem == null)
                return NotFound(new
                {
                    success = false,
                    message = "Product not found in cart"
                });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Removed from cart"
            });
        }

        [HttpPost("order/checkout")]
        public async Task<IActionResult> Checkout([FromQuery] int userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest(new { success = false, message = "Cart is empty" });

            var order = new Order
            {
                UserId = userId,
                TotalAmount = cartItems.Sum(c => c.Product!.Price * c.Quantity),
                Items = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    Price = c.Product!.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                total = order.TotalAmount
            });
        }

        [HttpGet("order/user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderResponse
                {
                    OrderId = o.Id,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            if (!orders.Any())
                return NotFound(new { message = "No orders found" });

            return Ok(orders);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            return Ok(new
            {
                orderId = order.Id,
                createdAt = order.CreatedAt,
                total = order.TotalAmount,
                status = order.Status,
                items = order.Items
            });
        }
    }
}