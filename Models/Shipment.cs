using System.ComponentModel.DataAnnotations;

namespace CongCuGoiYSanPham.Models
{
    public class Shipment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string TrackingNumber { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Carrier { get; set; } // GHN, GHTK, VNPost, etc.
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Preparing"; // Preparing, Shipped, InTransit, Delivered, Returned
        
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        
        [MaxLength(500)]
        public string Notes { get; set; }
    }
}
