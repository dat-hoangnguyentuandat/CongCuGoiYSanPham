using System.ComponentModel.DataAnnotations;

namespace CongCuGoiYSanPham.Models
{
    public class Inventory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int VariantId { get; set; }
        public virtual Variant Variant { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public int ReorderLevel { get; set; } = 10;
        
        public DateTime LastRestockedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
