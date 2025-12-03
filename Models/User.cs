using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CongCuGoiYSanPham.Models
{
    public class User : IdentityUser
    {
        [MaxLength(100)]
        public string? FullName { get; set; }
        
        [MaxLength(200)]
        public string? Address { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();
    }
}