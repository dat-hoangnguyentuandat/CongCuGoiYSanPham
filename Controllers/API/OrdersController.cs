using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongCuGoiYSanPham.Models;
using CongCuGoiYSanPham.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace CongCuGoiYSanPham.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Sử dụng default authentication (Cookie)
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            var userId = GetUserId();
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.OrderItems.Sum(oi => oi.Quantity)
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDetailDto>> GetOrder(int id)
        {
            var userId = GetUserId();
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            var dto = new OrderDetailDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                ShippingAddress = order.ShippingAddress,
                PhoneNumber = order.PhoneNumber,
                DiscountAmount = order.DiscountAmount,
                ShippingFee = order.ShippingFee,
                ItemCount = order.OrderItems.Sum(oi => oi.Quantity),
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.Variant.Product.Id,
                    ProductName = oi.Variant.Product.Name,
                    VariantInfo = $"{oi.Variant.Size} - {oi.Variant.Color}",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    DiscountAmount = oi.DiscountAmount,
                    TotalPrice = (oi.UnitPrice * oi.Quantity) - oi.DiscountAmount
                }).ToList(),
                Payment = order.Payment != null ? new PaymentDto
                {
                    Id = order.Payment.Id,
                    PaymentMethod = order.Payment.PaymentMethod,
                    Amount = order.Payment.Amount,
                    Status = order.Payment.Status,
                    TransactionId = order.Payment.TransactionId,
                    CreatedAt = order.Payment.CreatedAt,
                    CompletedAt = order.Payment.CompletedAt
                } : null,
                Shipment = order.Shipment != null ? new ShipmentDto
                {
                    Id = order.Shipment.Id,
                    TrackingNumber = order.Shipment.TrackingNumber,
                    Carrier = order.Shipment.Carrier,
                    Status = order.Shipment.Status,
                    ShippedAt = order.Shipment.ShippedAt,
                    DeliveredAt = order.Shipment.DeliveredAt,
                    Notes = order.Shipment.Notes
                } : null
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDetailDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                // Check model validation
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    return BadRequest(new { message = "Dữ liệu không hợp lệ", errors });
                }

                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { message = "Không thể xác định người dùng" });

                var cart = await _context.Carts
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Variant).ThenInclude(v => v.Inventory)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    return BadRequest(new { message = "Giỏ hàng trống" });

                // Check inventory
                foreach (var item in cart.CartItems)
                {
                    if (item.Variant.Inventory == null || item.Variant.Inventory.Quantity < item.Quantity)
                        return BadRequest(new { message = $"Không đủ hàng cho sản phẩm {item.Variant.SKU}" });
                }

                var order = new Order
                {
                    OrderNumber = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}",
                    UserId = userId,
                    ShippingAddress = dto.ShippingAddress,
                    PhoneNumber = dto.PhoneNumber,
                    ShippingFee = 30000,
                    Status = "Pending",
                    OrderItems = new List<OrderItem>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Apply promotion if provided
                if (!string.IsNullOrEmpty(dto.PromotionCode))
                {
                    var promotion = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.Code == dto.PromotionCode && p.IsActive 
                            && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);

                    if (promotion != null)
                    {
                        order.PromotionId = promotion.Id;
                        var subtotal = cart.CartItems.Sum(ci => (ci.Variant.DiscountPrice ?? ci.Variant.Price) * ci.Quantity);
                        
                        if (promotion.DiscountType == "Percentage")
                        {
                            order.DiscountAmount = subtotal * (promotion.DiscountValue / 100);
                            if (promotion.MaxDiscountAmount.HasValue)
                                order.DiscountAmount = Math.Min(order.DiscountAmount, promotion.MaxDiscountAmount.Value);
                        }
                        else
                        {
                            order.DiscountAmount = promotion.DiscountValue;
                        }
                    }
                }

                // Create order items và update inventory
                foreach (var cartItem in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        VariantId = cartItem.VariantId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Variant.DiscountPrice ?? cartItem.Variant.Price
                    };
                    order.OrderItems.Add(orderItem);

                    // Update inventory safely
                    if (cartItem.Variant.Inventory != null)
                    {
                        cartItem.Variant.Inventory.Quantity -= cartItem.Quantity;
                        cartItem.Variant.Inventory.UpdatedAt = DateTime.UtcNow;
                    }
                }

                order.TotalAmount = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity) - order.DiscountAmount + order.ShippingFee;

                // Create payment
                order.Payment = new Payment
                {
                    PaymentMethod = dto.PaymentMethod,
                    Amount = order.TotalAmount,
                    Status = dto.PaymentMethod == "COD" ? "Pending" : "Completed",
                    TransactionId = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}",
                    CreatedAt = DateTime.UtcNow
                };

                // Create shipment
                order.Shipment = new Shipment
                {
                    TrackingNumber = $"TRK{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Carrier = "GHN",
                    Status = "Preparing",
                    Notes = ""
                };

                // Sử dụng transaction để đảm bảo data consistency
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Orders.Add(order);
                    _context.CartItems.RemoveRange(cart.CartItems);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new { orderId = order.Id, orderNumber = order.OrderNumber });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                var fullMessage = $"Lỗi server: {ex.Message}. Inner: {innerMessage}";
                
                return BadRequest(new { message = fullMessage });
            }
        }

        [HttpPost("direct")]
        public async Task<ActionResult<OrderDetailDto>> CreateDirectOrder([FromBody] CreateDirectOrderDto dto)
        {
            try
            {
                // Check model validation
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    return BadRequest(new { message = "Dữ liệu không hợp lệ", errors });
                }

                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { message = "Không thể xác định người dùng" });

                // Validate items and check inventory
                foreach (var item in dto.Items)
                {
                    var variant = await _context.Variants
                        .Include(v => v.Inventory)
                        .FirstOrDefaultAsync(v => v.Id == item.VariantId);

                    if (variant == null)
                        return BadRequest(new { message = $"Không tìm thấy sản phẩm với ID {item.VariantId}" });

                    if (variant.Inventory == null || variant.Inventory.Quantity < item.Quantity)
                        return BadRequest(new { message = $"Không đủ hàng cho sản phẩm {variant.SKU}" });
                }

                var order = new Order
                {
                    OrderNumber = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}",
                    UserId = userId,
                    ShippingAddress = dto.ShippingAddress,
                    PhoneNumber = dto.PhoneNumber,
                    ShippingFee = 30000,
                    Status = "Pending",
                    OrderItems = new List<OrderItem>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Apply promotion if provided
                if (!string.IsNullOrEmpty(dto.PromotionCode))
                {
                    var promotion = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.Code == dto.PromotionCode && p.IsActive 
                            && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);

                    if (promotion != null)
                    {
                        order.PromotionId = promotion.Id;
                        var subtotal = dto.Items.Sum(i => i.UnitPrice * i.Quantity);
                        
                        if (promotion.DiscountType == "Percentage")
                        {
                            order.DiscountAmount = subtotal * (promotion.DiscountValue / 100);
                            if (promotion.MaxDiscountAmount.HasValue)
                                order.DiscountAmount = Math.Min(order.DiscountAmount, promotion.MaxDiscountAmount.Value);
                        }
                        else
                        {
                            order.DiscountAmount = promotion.DiscountValue;
                        }
                    }
                }

                // Create order items and update inventory
                foreach (var item in dto.Items)
                {
                    var variant = await _context.Variants
                        .Include(v => v.Inventory)
                        .FirstOrDefaultAsync(v => v.Id == item.VariantId);

                    var orderItem = new OrderItem
                    {
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    order.OrderItems.Add(orderItem);

                    // Update inventory
                    if (variant.Inventory != null)
                    {
                        variant.Inventory.Quantity -= item.Quantity;
                        variant.Inventory.UpdatedAt = DateTime.UtcNow;
                    }
                }

                order.TotalAmount = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity) - order.DiscountAmount + order.ShippingFee;

                // Create payment
                order.Payment = new Payment
                {
                    PaymentMethod = dto.PaymentMethod,
                    Amount = order.TotalAmount,
                    Status = dto.PaymentMethod == "COD" ? "Pending" : "Completed",
                    TransactionId = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}",
                    CreatedAt = DateTime.UtcNow
                };

                // Create shipment
                order.Shipment = new Shipment
                {
                    TrackingNumber = $"TRK{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Carrier = "GHN",
                    Status = "Preparing",
                    Notes = ""
                };

                // Use transaction to ensure data consistency
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new { orderId = order.Id, orderNumber = order.OrderNumber });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                var fullMessage = $"Lỗi server: {ex.Message}. Inner: {innerMessage}";
                
                return BadRequest(new { message = fullMessage });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<ActionResult> CancelOrder(int id)
        {
            var userId = GetUserId();
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            if (order.Status != "Pending" && order.Status != "Confirmed")
                return BadRequest("Cannot cancel order in current status");

            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            // Restore inventory
            foreach (var item in order.OrderItems)
            {
                if (item.Variant.Inventory != null)
                {
                    item.Variant.Inventory.Quantity += item.Quantity;
                    item.Variant.Inventory.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order cancelled" });
        }

        [HttpGet("promotions/validate")]
        [AllowAnonymous]
        public async Task<ActionResult> ValidatePromotion([FromQuery] string code, [FromQuery] decimal? subtotal = null)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest(new { message = "Mã khuyến mãi không được để trống" });

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper() && p.IsActive);

            if (promotion == null)
                return NotFound(new { message = "Mã khuyến mãi không tồn tại hoặc đã bị vô hiệu hóa" });

            var now = DateTime.UtcNow;
            if (promotion.StartDate > now)
                return BadRequest(new { message = $"Mã khuyến mãi chưa có hiệu lực. Bắt đầu từ {promotion.StartDate:dd/MM/yyyy}" });

            if (promotion.EndDate < now)
                return BadRequest(new { message = $"Mã khuyến mãi đã hết hạn. Kết thúc vào {promotion.EndDate:dd/MM/yyyy}" });

            if (promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit.Value)
                return BadRequest(new { message = "Mã khuyến mãi đã hết lượt sử dụng" });

            // Check minimum order amount if subtotal is provided
            if (subtotal.HasValue && promotion.MinOrderAmount.HasValue && subtotal.Value < promotion.MinOrderAmount.Value)
                return BadRequest(new { message = $"Đơn hàng tối thiểu {promotion.MinOrderAmount.Value:N0}đ để sử dụng mã này" });

            // Calculate discount amount
            decimal discountAmount = 0;
            if (subtotal.HasValue)
            {
                if (promotion.DiscountType == "Percentage")
                {
                    discountAmount = subtotal.Value * (promotion.DiscountValue / 100);
                    if (promotion.MaxDiscountAmount.HasValue)
                        discountAmount = Math.Min(discountAmount, promotion.MaxDiscountAmount.Value);
                }
                else
                {
                    discountAmount = promotion.DiscountValue;
                }
            }

            return Ok(new
            {
                id = promotion.Id,
                code = promotion.Code,
                name = promotion.Name,
                description = promotion.Description,
                discountType = promotion.DiscountType,
                discountValue = promotion.DiscountValue,
                maxDiscountAmount = promotion.MaxDiscountAmount,
                discountAmount = discountAmount,
                minOrderAmount = promotion.MinOrderAmount
            });
        }
    }
}