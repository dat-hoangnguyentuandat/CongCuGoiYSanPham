using System.ComponentModel.DataAnnotations;

namespace CongCuGoiYSanPham.Models.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
    }

    public class OrderDetailDto : OrderDto
    {
        public string ShippingAddress { get; set; }
        public string PhoneNumber { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public List<OrderItemDto> Items { get; set; }
        public PaymentDto Payment { get; set; }
        public ShipmentDto Shipment { get; set; }
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string VariantInfo { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
        [StringLength(500, ErrorMessage = "Địa chỉ giao hàng không được quá 500 ký tự")]
        public string ShippingAddress { get; set; }
        
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
        public string PaymentMethod { get; set; }
        
        public string? PromotionCode { get; set; }
    }

    public class CreateDirectOrderDto
    {
        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
        [StringLength(500, ErrorMessage = "Địa chỉ giao hàng không được quá 500 ký tự")]
        public string ShippingAddress { get; set; }
        
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
        public string PaymentMethod { get; set; }
        
        public string? PromotionCode { get; set; }
        
        [Required(ErrorMessage = "Danh sách sản phẩm không được để trống")]
        public List<DirectOrderItemDto> Items { get; set; }
    }

    public class DirectOrderItemDto
    {
        [Required]
        public int VariantId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal UnitPrice { get; set; }
    }
}
