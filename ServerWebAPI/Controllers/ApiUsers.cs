using Microsoft.AspNetCore.Mvc;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;

namespace ServerWebAPI.Controllers
{
    [Route("api/users")] // Route theo chuẩn cũ
    [ApiController]
    public class ApiUsers : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiUsers(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found", Status = false });
            }

            return Ok(new
            {
                Message = "Success",
                Status = true,
                Data = new
                {
                    user.Id,
                    user.Username,
                    user.Fullname,
                    user.Email,
                    user.Phone,
                    user.Address,
                    // Bỏ Age, Birth vì DB mới không có
                    Role = "User" // Giữ nguyên hardcode như file gốc
                }
            });
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateProfile(int id, [FromBody] UpdateUserDTO dto)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound(new { Message = "User not found", Status = false });

            user.Fullname = dto.Fullname;
            user.Email = dto.Email;
            user.Phone = dto.Phone;
            user.Address = dto.Address;

            _context.SaveChanges();

            return Ok(new { Message = "Update success", Status = true });
        }

        // PUT: api/users/{id}/change-password
        [HttpPut("{id}/change-password")]
        public IActionResult ChangePassword(int id, [FromBody] ChangePasswordDTO dto)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            // Check pass cũ (phải hash mới so sánh được)
            string oldPassHash = ApiAuth.HashPassword(dto.OldPassword);

            if (user.PasswordHash != oldPassHash)
            {
                return BadRequest(new { Message = "Old password incorrect", Status = false });
            }

            user.PasswordHash = ApiAuth.HashPassword(dto.NewPassword);
            _context.SaveChanges();

            return Ok(new { Message = "Password changed successfully", Status = true });
        }

        // PUT: api/users/{id}/avatar (Giữ nguyên tính năng cũ)
        [HttpPut("{id}/avatar")]
        public IActionResult UploadAvatar(int id, IFormFile avatar)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound(new { Message = "User not found", Status = false });
            }

            // Demo: fake URL avatar như code cũ
            string avatarUrl = $"https://server/avatar/user_{id}.jpg";

            // Nếu muốn lưu tên file vào DB thì uncomment dòng dưới:
            // user.Avatar = avatarUrl; 
            // _context.SaveChanges();

            return Ok(new
            {
                Message = "Upload avatar success",
                Status = true,
                Avatar = avatarUrl
            });
        }
    }

    // ==========================================
    // DTO CLASSES
    // ==========================================
    public class UpdateUserDTO
    {
        public string? Fullname { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class ChangePasswordDTO
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}