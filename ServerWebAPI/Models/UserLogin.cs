using System.ComponentModel.DataAnnotations;

namespace ServerWebAPI.Models
{
    public class UserLogin
    {
        [Required]
        public string Username { get; set; }
            
        [Required]
        public string Password { get; set; }
    }
}