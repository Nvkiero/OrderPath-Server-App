using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ServerWebAPI.Controllers
{
    [Route("auth")]
    [ApiController]
    public class ApiAuth : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Giữ nguyên logic lưu OTP trong Ram như code cũ của bạn
        private static ConcurrentDictionary<string, OtpInfo> OtpStore = new();

        public ApiAuth(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Helper: Hash Password (để dùng chung)
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Helper: Tạo OTP ngẫu nhiên (như code cũ)
        private string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        // POST: auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO req)
        {
            if (await _context.Users.AnyAsync(u => u.Username == req.Username))
                return BadRequest(new { message = "Username đã tồn tại" });

            var newUser = new User
            {
                Username = req.Username,
                PasswordHash = HashPassword(req.Password), // Lưu pass đã mã hóa
                Fullname = req.Fullname,
                Email = req.Email,
                Phone = req.Phone,
                Address = req.Address
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công", userId = newUser.Id });
        }

        // POST: auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO req)
        {
            string hashInfo = HashPassword(req.Password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == req.Username && u.PasswordHash == hashInfo);

            if (user == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            // Logic phân quyền (Seller/Shipper)
            string role = "Customer";
            int entityId = 0;

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (shop != null) { role = "Seller"; entityId = shop.Id; }

            if (req.RoleRequest == "Shipper")
            {
                var shipper = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
                // Lưu ý: Logic shipper check tạm thời check Shop bảng, bạn cần bảng Shipper nếu có
                // Ở đây mình check bảng Shippers theo DB mới
                var realShipper = await _context.Shippers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (realShipper != null) { role = "Shipper"; entityId = realShipper.Id; }
            }

            var token = GenerateJwtToken(user, role, entityId);

            return Ok(new
            {
                message = "Đăng nhập thành công",
                userId = user.Id,
                username = user.Username,
                role,
                token
            });
        }

        // POST: auth/send-otp (Giữ nguyên tính năng cũ)
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
                return BadRequest(new { status = false, message = "Email không hợp lệ" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
                return NotFound(new { status = false, message = "Email không tồn tại trong hệ thống" });

            string otp = GenerateOtp();

            OtpStore[model.Email] = new OtpInfo
            {
                Code = otp,
                ExpiredAt = DateTime.Now.AddMinutes(5)
            };

            // Demo in ra console (thực tế gửi mail)
            Console.WriteLine($"OTP của {model.Email}: {otp}");

            return Ok(new { status = true, message = "OTP đã được gửi (Check Console server)" });
        }

        // POST: auth/forgot-password (Giữ nguyên tính năng cũ)
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (!OtpStore.ContainsKey(model.Email))
                return BadRequest(new { status = false, message = "Vui lòng yêu cầu gửi OTP trước" });

            var otpInfo = OtpStore[model.Email];
            if (otpInfo.Code != model.OtpCode)
                return BadRequest(new { status = false, message = "Mã OTP không đúng" });

            if (DateTime.Now > otpInfo.ExpiredAt)
                return BadRequest(new { status = false, message = "Mã OTP đã hết hạn" });

            // OTP đúng -> Đổi mật khẩu
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user != null)
            {
                user.PasswordHash = HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();
                OtpStore.TryRemove(model.Email, out _); // Xóa OTP sau khi dùng
                return Ok(new { status = true, message = "Đặt lại mật khẩu thành công" });
            }

            return BadRequest(new { status = false, message = "Lỗi xử lý" });
        }

        private string GenerateJwtToken(User user, string role, int entityId)
        {
            var keyStr = _configuration["Jwt:Key"] ?? "Key_Must_Be_Very_Long_For_Security_Reasons_32Chars";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, role),
                new Claim("EntityId", entityId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // ==========================================
    // DTO CLASSES
    // ==========================================
    public class OtpInfo
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
    }

    public class RegisterDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class LoginDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? RoleRequest { get; set; }
    }

    public class SendOtpRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}