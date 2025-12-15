using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace ServerWebAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Thêm biến thành viên IConfiguration

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        // Đăng kí
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegister user)
        {
            try
            {
                if (user == null)
                    return BadRequest("");
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
                    return BadRequest(ex.Message);
                }
                return Ok(); //có thể thay như thế này để thống nhất return Ok(new { status = true, message = "Đăng ký thành công" });

            }
            catch (Exception ex) { 
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin login)
        {
            try
            {
                if (login == null)
                    return BadRequest("Nhập thông tin đầy đủ.");

                string hashedPassword = HashPassword(login.Password);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == login.Username && u.Password == hashedPassword);

                if (user == null)
                    return Unauthorized("Sai tên hoặc password.");

                var token = GenerateJwtToken(user);

                return Ok(new { message = "Đăng nhập thành công", userId = user.Id, token });
            }
            catch (Exception ex)
            {
                return BadRequest();
            }

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
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Issuer"],
                audience: HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    double.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:ExpiresInMinutes"])),
                signingCredentials: creds);

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

                // Kiểm tra OTP
                string correctOtp = _configuration["OtpSettings:DefaultOtp"];
                // Kiểm tra OTP có được cấu hình không
                if (string.IsNullOrEmpty(correctOtp))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        message = "OTP chưa được cấu hình"
                    });
                }

                if (model.OTP != correctOtp)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "OTP không đúng hoặc đã hết hạn"
                    });
                }

                // Cập nhật mật khẩu mới
                user.Password = HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

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
    }
}
