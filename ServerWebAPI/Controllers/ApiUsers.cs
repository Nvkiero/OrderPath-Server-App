using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerWebAPI.DataBase;
using ServerWebAPI.Models;
using System.Security.Claims;

namespace ServerWebAPI.Controllers
{
    [Route("users")]
    [ApiController]
    [Authorize] // Bắt buộc phải có Token mới vào được
    public class ApiUsers : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiUsers(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserIdFromToken()    
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // GET: users/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            int userId = GetUserIdFromToken();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            return Ok(new UserResponse
            {
                Status = true,
                Message = "Success",
                ID = user.Id,
                Username = user.Username,
                FullName = user.Fullname,
                Birth = user.Birth,
                Age = user.Age,
                Email = user.Email,
                Address = user.Address,
                Role = user.Role,
                Phone = user.Phone,
                Avatar = user.AvatarUrl
            });
        }

        // PUT: users/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDTO dto)
        {
            int userId = GetUserIdFromToken();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            // Cập nhật các trường nếu có dữ liệu gửi lên
            if (!string.IsNullOrEmpty(dto.Fullname)) user.Fullname = dto.Fullname;
            if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Phone)) user.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.Address)) user.Address = dto.Address;

            // Cập nhật Age và Birth (đã thêm vào DTO)
            if (dto.Age.HasValue) user.Age = dto.Age.Value;
            if (dto.Birth.HasValue) user.Birth = dto.Birth.Value;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Update success", Status = true });
        }
// PUT: api/users/me/change-password
[HttpPut("me/change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            int userId = GetUserIdFromToken();

            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            string oldHash = ApiAuth.HashPassword(dto.OldPassword);
            if (user.PasswordHash != oldHash)
            {
                return BadRequest(new { Message = "Old password incorrect", Status = false });
            }

            user.PasswordHash = ApiAuth.HashPassword(dto.NewPassword);
            _context.SaveChanges();

            return Ok(new { Message = "Password changed successfully", Status = true });
        }

        // PUT: api/users/me/avatar
        [HttpPut("me/avatar")]
        public IActionResult UploadAvatar(IFormFile avatar)
        {
            int userId = GetUserIdFromToken();

            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound(new { Message = "User not found", Status = false });

            string avatarUrl = $"https://server/avatar/user_{userId}.jpg";

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
}
