using System.ComponentModel.DataAnnotations;

namespace CongCuGoiYSanPham.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        
        [Required]
        public string UserId { get; set; }
        public virtual User User { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [MaxLength(1000)]
        public string Comment { get; set; }
        
        public bool IsVerifiedPurchase { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
