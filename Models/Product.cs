using System.ComponentModel.DataAnnotations;

namespace CongCuGoiYSanPham.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        
        [MaxLength(1000)]
        public string Description { get; set; }
        
        [MaxLength(500)]
        public string ImageUrl { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Variant> Variants { get; set; } = new List<Variant>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
