using Microsoft.AspNetCore.Authorization;
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
            // kiem tra username da ton tai
            if (await _context.Users.AnyAsync(u => u.Username == req.Username))
                return BadRequest(new { message = "Username đã tồn tại" });
            // tao user moi
            var newUser = new User
            {
                Username = req.Username,
                PasswordHash = HashPassword(req.Password), // Lưu pass đã mã hóa
                Fullname = req.Fullname,
                Email = req.Email,
                Phone = req.Phone,
                Address = req.Address
            };
            // luu vao db
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công", userId = newUser.Id });
        }

        // POST: auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO req)
        {
            var hashInfo = HashPassword(req.Password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == req.Username && u.PasswordHash == hashInfo);

            if (user == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            string role = "Customer";
            int entityId = 0;

            var shipper = await _context.Shippers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (shipper != null)
            {
                role = "Shipper";
                entityId = shipper.Id;
            }
            else
            {
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (shop != null)
                {
                    role = "Seller";
                    entityId = shop.Id;
                }
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
            var keyStr = _configuration["Jwt:Key"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, role),
                new Claim("entityId", entityId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(4),  
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

                // Lấy userId từ token, jwt đã được xác thực bởi [Authorize]
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { status = false, message = "Token không hợp lệ" });

                int userId = int.Parse(userIdClaim.Value);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { status = false, message = "Không tìm thấy người dùng" });

                // Kiểm tra mật khẩu cũ
                if (user.PasswordHash != HashPassword(model.OldPassword))
                {
                    return BadRequest(new { status = false, message = "Mật khẩu cũ không đúng" });
                }

                // Mật khẩu mới không được rỗng
                if (string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    return BadRequest(new { status = false, message = "Mật khẩu mới không hợp lệ" });
                }

                // Mật khẩu mới phải khác mật khẩu cũ
                if (HashPassword(model.NewPassword) == user.PasswordHash)
                {
                    return BadRequest(new { status = false, message = "Mật khẩu mới phải khác mật khẩu cũ" });
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = HashPassword(model.NewPassword);
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
                user.PasswordHash = HashPassword(model.NewPassword);
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


        // ==========================================
        // DTO CLASSES
        // ==========================================
        public class OtpInfo
        {
            public string Code { get; set; } = string.Empty;
            public DateTime ExpiredAt { get; set; }
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
}