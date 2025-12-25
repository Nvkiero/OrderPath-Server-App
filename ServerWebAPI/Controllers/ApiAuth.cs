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
using MailKit.Net.Smtp;
using MimeKit;

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
            if (req == null)
                return BadRequest(new { message = "Dữ liệu không hợp lệ" });

            req.Username = req.Username?.Trim().ToLower();
            req.Email = req.Email?.Trim().ToLower();

            // Kiểm tra role hợp lệ trước
            if (req.Role != "Customer" && req.Role != "Seller" && req.Role != "Shipper")
                return BadRequest(new { message = "Role không hợp lệ" });

            // Kiểm tra trùng Username hoặc Email
            var existsUser = await _context.Users
                .Where(u => u.Username == req.Username || u.Email == req.Email)
                .Select(u => new { u.Username, u.Email })
                .FirstOrDefaultAsync();

            if (existsUser != null)
            {
                if (existsUser.Username == req.Username)
                    return BadRequest(new { field = "username", message = "Username đã tồn tại" });

                if (existsUser.Email == req.Email)
                    return BadRequest(new { field = "email", message = "Email đã được sử dụng" });
            }

            var user = new User
            {
                Username = req.Username,
                PasswordHash = HashPassword(req.Password),
                Fullname = req.Fullname,
                Email = req.Email,
                Birth = req.Birth,
                Phone = req.Phone,
                Age = req.Age,
                Address = req.Address,
                Role = req.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (req.Role == "Shipper")
            {
                _context.Shippers.Add(new Shipper { UserId = user.Id });
            }
            else if (req.Role == "Seller")
            {
                _context.Shops.Add(new Shop
                {
                    UserId = user.Id,
                    ShopName = $"{user.Username}'s shop"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công" });
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
        private string GenerateJwtToken(User user, string role, int entityId)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
             {
                 new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                 new Claim(ClaimTypes.Name, user.Username),
                 new Claim(ClaimTypes.Role, role),

                 // Optional: entityId nếu role khác Customer
                 new Claim("entityId", entityId.ToString()),

                 // JWT ID để tránh replay attack
                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
             };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpiresInMinutes"]!)
                ),
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

                if (string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    return BadRequest(new { status = false, message = "Mật khẩu mới không hợp lệ" });
                }

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

            // GỬI OTP QUA GMAIL
            await SendOtpEmailAsync(model.Email, otp);

            return Ok(new
            {
                message = "OTP đã được gửi tới email"
            });
        }

        private async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                "OrderPath System",
                _configuration["Gmail:Email"]
            ));

            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Mã OTP đặt lại mật khẩu";

            message.Body = new TextPart("html")
            {
                Text = $@"
            <h3>Xin chào!</h3>
            <p>Mã OTP của bạn là:</p>
            <h2 style='color:red'>{otp}</h2>
            <p>OTP có hiệu lực trong 5 phút.</p>
        "
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, false);
            await smtp.AuthenticateAsync(
                _configuration["Gmail:Email"],
                _configuration["Gmail:AppPassword"]
            );
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

    }
}