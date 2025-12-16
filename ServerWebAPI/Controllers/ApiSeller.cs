using Microsoft.AspNetCore.Mvc;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models.Seller;
using ServerWebAPI.Models.Seller.QuanLyDonHang;
using System;
using System.Collections.Generic;

namespace ServerWebAPI.Controllers
{
    [ApiController]
    [Route("seller")]
    public class ApiSellerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiSellerController(AppDbContext context)
        {
            _context = context;
        }

        // Thêm sản phẩm
        // POST /seller/products
        [HttpPost("products")]
        public IActionResult AddProduct([FromBody] AddProduct dto)
        {
            // Demo (chưa lưu DB thật)
            int newProductId = new Random().Next(1000, 9999);

            return Ok(new
            {
                ProductId = newProductId,
                Status = "Success",
                Error = false
            });
        }

        // Sửa sản phẩm
        // PUT /seller/products/{id}
        [HttpPut("products/{id}")]
        public IActionResult UpdateProduct(int id, [FromBody] ChangeProduct dto)
        {
            if (id != dto.id)
            {
                return BadRequest(new
                {
                    Status = "Failed",
                    Error = true
                });
            }

            return Ok(new
            {
                Status = "Success",
                Error = false
            });
        }

        // Xóa sản phẩm
        // PUT /seller/products/{id}/delete
        [HttpPut("products/{id}/delete")]
        public IActionResult DeleteProduct(int id)
        {
            return Ok(new
            {
                Status = "Success",
                Error = false
            });
        }

        // Lấy danh sách sản phẩm của shop
        // GET /seller/products
        [HttpGet("products")]
        public IActionResult GetProductList()
        {
            var products = new List<object>
            {
                new {
                    Id = 1,
                    Name = "Áo thun",
                    Price = 120000,
                    Quantity = 20,
                    Image = "https://img.demo/ao.jpg",
                    Status = true
                },
                new {
                    Id = 2,
                    Name = "Quần jean",
                    Price = 300000,
                    Quantity = 10,
                    Image = "https://img.demo/quan.jpg",
                    Status = false
                }
            };

            return Ok(products);
        }

        // =========================
        // QUẢN LÝ ĐƠN HÀNG
        // =========================

        // Lấy danh sách đơn hàng của shop
        // GET /seller/orders
        [HttpGet("orders")]
        public IActionResult GetOrders()
        {
            var orders = new List<GetOrder>
            {
                new GetOrder
                {
                    OrderId = 1,
                    CustomerId = 101,
                    Total = 500000,
                    Status = "NEW",
                    Date = DateTime.Now
                },
                new GetOrder
                {
                    OrderId = 2,
                    CustomerId = 102,
                    Total = 1200000,
                    Status = "CONFIRMED",
                    Date = DateTime.Now.AddDays(-1)
                }
            };

            return Ok(orders);
        }

        // Xác nhận đơn hàng
        // PUT /seller/orders/{id}/confirm
        [HttpPut("orders/{id}/confirm")]
        public IActionResult ConfirmOrder(int id)
        {
            return Ok(new
            {
                Status = "Confirmed",
                Error = false,
                ShipperOrderId = new Random().Next(1000, 9999)
            });
        }

        // Hủy đơn hàng
        // PUT /seller/orders/{id}/cancel
        [HttpPut("orders/{id}/cancel")]
        public IActionResult CancelOrder(int id)
        {
            return Ok(new
            {
                Status = "Cancelled",
                Error = false
            });
        }
    }
}
