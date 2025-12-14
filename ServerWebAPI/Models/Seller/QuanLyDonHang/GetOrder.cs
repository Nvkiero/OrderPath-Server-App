namespace ServerWebAPI.Models.Seller.QuanLyDonHang
{
    public class GetOrder
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public double Total {  get; set; }
        public string Status { get; set; } = "";
        public DateTime Date { get; set; }
    }
}
