namespace ServerWebAPI.Models
{
    public class UpdateUserDTO
    {
        public string? Fullname { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? Birth { get; set; }
        public int? Age { get; set; }
    }
}
