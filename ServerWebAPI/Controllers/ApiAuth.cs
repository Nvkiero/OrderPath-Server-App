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
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Thêm biến thành viên IConfiguration
        private static Dictionary<string, OtpInfo> OtpStore = new();

        // Hàm tạo OTP ngẫu nhiên
        private string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString();
        }

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
                    return BadRequest(ex.Message);
                }
                return Ok(); 

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
                    OtpStore.Remove(model.Email);
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

                OtpStore.Remove(model.Email);

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
        public async Task<IActionResult> SendOtp([FromBody] string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound(new { status = false, message = "Email không tồn tại" });

            string otp = GenerateOtp();

            OtpStore[email] = new OtpInfo
            {
                Code = otp,
                ExpiredAt = DateTime.Now.AddMinutes(5)
            };

            // Demo gửi OTP qua email bằng cách in ra console
            Console.WriteLine($"OTP của {email}: {otp}");

            return Ok(new
            {
                status = true,
                message = "OTP đã được gửi"
            });
        }

    }
}
