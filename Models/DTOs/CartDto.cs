namespace CongCuGoiYSanPham.Models.DTOs
{
    public class CartDto
    {
        public int Id { get; set; }
        public List<CartItemDto> Items { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public string ProductName { get; set; }
        public string VariantInfo { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string ImageUrl { get; set; }
    }

    public class AddToCartDto
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
