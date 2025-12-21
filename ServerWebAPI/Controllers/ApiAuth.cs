using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ServerWebAPI.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Thêm biến thành viên IConfiguration, nếu không dùng có thể bỏ
        private static ConcurrentDictionary<string, OtpInfo> OtpStore = new();

        // Hàm tạo OTP ngẫu nhiên
        private string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;//nếu không dùng có thể bỏ
        }
        // Đăng kí
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegister user)
        {
            if (user == null)
                return BadRequest();
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return BadRequest();

            var newUser = new User
            {
                Username = user.Username,
                Password = HashPassword(user.Password),
                Fullname = user.Fullname,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Age = user.Age,
                Birth = user.Birth,
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin login)
        {
            if (login == null)
                return BadRequest(new { message = "không nhận được thông điệp của client" });

            string hashedPassword = HashPassword(login.Password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == login.Username &&
                    u.Password == hashedPassword);

            if (user == null)
                return Unauthorized(new { message = "Sai username hoặc mật khẩu" });

            string role = "Buyer";

            if (login.Role == "Shop")
            {
                if (!await _context.Shops.AnyAsync(s => s.UserId == user.Id))
                    return Unauthorized(new { message = "Tài khoản không phải Shop" });

                role = "Shop";
            }
            else if (login.Role == "Shipper")
            {
                if (!await _context.Shippers.AnyAsync(s => s.UserId == user.Id))
                    return Unauthorized(new { message = "Tài khoản không phải Shipper" });

                role = "Shipper";
            }

            var token = GenerateJwtToken(user, role);

            return Ok(new
            {
                message = "Đăng nhập thành công",
                userId = user.Id,
                role,
                token
            });
        }


        private static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        private string GenerateJwtToken(User user, string role)
        {
            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, role) // ⭐ QUAN TRỌNG
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    config.GetValue<int>("Jwt:ExpiresInMinutes")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { status = false, message = "Dữ liệu không hợp lệ" });

                // Lấy userId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = false, message = "Token không hợp lệ" });

                int userId = int.Parse(userIdClaim.Value);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { status = false, message = "Không tìm thấy người dùng" });

                // Kiểm tra mật khẩu cũ
                if (user.Password != HashPassword(model.OldPassword))
                {
                    return BadRequest(new { status = false, message = "Mật khẩu cũ không đúng" });
                }

                // Cập nhật mật khẩu mới
                user.Password = HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    status = true,
                    message = "Thay đổi mật khẩu thành công"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { status = false, message = "Dữ liệu không hợp lệ" });

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                    return NotFound(new { status = false, message = "Email không tồn tại" });

                // Kiểm tra OTP có tồn tại không
                if (!OtpStore.ContainsKey(model.Email))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "OTP không tồn tại hoặc đã hết hạn"
                    });
                }

                var otpInfo = OtpStore[model.Email];

                // Kiểm tra hết hạn
                if (otpInfo.ExpiredAt < DateTime.Now)
                {
                    OtpStore.TryRemove(model.Email, out _);
                    return BadRequest(new
                    {
                        status = false,
                        message = "OTP đã hết hạn"
                    });
                }

                // Kiểm tra đúng OTP
                if (otpInfo.Code != model.OTP)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "OTP không đúng"
                    });
                }

                // Cập nhật mật khẩu mới
                user.Password = HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                OtpStore.TryRemove(model.Email, out _);

                return Ok(new
                {
                    status = true,
                    message = "Đặt lại mật khẩu thành công"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
                return BadRequest(new { status = false, message = "Email không hợp lệ" });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
                return NotFound(new { status = false, message = "Email không tồn tại" });

            string otp = GenerateOtp();

            OtpStore[model.Email] = new OtpInfo
            {
                Code = otp,
                ExpiredAt = DateTime.Now.AddMinutes(5)
            };

            // Demo gửi OTP qua email bằng cách in ra console
            Console.WriteLine($"OTP của {model.Email}: {otp}");

            return Ok(new
            {
                status = true,
                message = "OTP đã được gửi"
            });
        }

    }
}
