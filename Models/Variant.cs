using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongCuGoiYSanPham.Models
{
    public class Variant
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SKU { get; set; }
        
        [MaxLength(100)]
        public string Size { get; set; }
        
        [MaxLength(50)]
        public string Color { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual Inventory Inventory { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
