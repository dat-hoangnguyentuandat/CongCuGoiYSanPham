using System.ComponentModel.DataAnnotations;

namespace CongCuGoiYSanPham.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        public int? ParentCategoryId { get; set; }
        public virtual Category ParentCategory { get; set; }
        
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
