namespace ServerWebAPI.Models.Shipper
{
    public enum ShipperStatus
    {
        Available,
        Delivery,
        Success
    }
    public class ShipperProfile
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public ShipperStatus ShipperStatus { get; set; }
    }
}
