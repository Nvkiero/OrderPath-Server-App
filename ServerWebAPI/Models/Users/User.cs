using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWebAPI.Models { 
    public class User
    {
        [Key] // Khai báo khóa chính
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Tự động tăng
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Birth { get; set; }
        public string Phone { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Address { get; set; } = string.Empty;
    }
}
