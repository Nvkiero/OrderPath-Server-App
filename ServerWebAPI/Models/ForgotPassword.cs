using System.ComponentModel.DataAnnotations;

namespace ServerWebAPI.Controllers
{
    public class UserLogin
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}