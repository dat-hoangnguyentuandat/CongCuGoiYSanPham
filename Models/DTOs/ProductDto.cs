namespace CongCuGoiYSanPham.Models.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryName { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class ProductDetailDto : ProductDto
    {
        public List<VariantDto> Variants { get; set; }
        public List<ReviewDto> Reviews { get; set; }
    }

    public class VariantDto
    {
        public int Id { get; set; }
        public string SKU { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
    }
}
