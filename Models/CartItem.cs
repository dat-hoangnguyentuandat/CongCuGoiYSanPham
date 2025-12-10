using System.ComponentModel.DataAnnotations;

namespace CongCuGoiYSanPham.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CartId { get; set; }
        public virtual Cart Cart { get; set; }
        
        [Required]
        public int VariantId { get; set; }
        public virtual Variant Variant { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
