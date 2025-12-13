namespace CongCuGoiYSanPham.Models.DTOs
{
    public class ShipmentDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; }
        public string Carrier { get; set; }
        public string Status { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string Notes { get; set; }
    }
}
