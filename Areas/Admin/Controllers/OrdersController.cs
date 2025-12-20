using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongCuGoiYSanPham.Models;

namespace CongCuGoiYSanPham.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllOrders([FromQuery] string status = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    CustomerName = o.User.FullName,
                    CustomerEmail = o.User.Email,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt,
                    ItemCount = o.OrderItems.Sum(oi => oi.Quantity)
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetOrderDetail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return Ok(new
            {
                order.Id,
                order.OrderNumber,
                Customer = new
                {
                    order.User.FullName,
                    order.User.Email,
                    order.User.PhoneNumber
                },
                order.TotalAmount,
                order.DiscountAmount,
                order.ShippingFee,
                order.Status,
                order.ShippingAddress,
                order.PhoneNumber,
                order.CreatedAt,
                Items = order.OrderItems.Select(oi => new
                {
                    oi.Id,
                    ProductName = oi.Variant.Product.Name,
                    VariantInfo = $"{oi.Variant.Size} - {oi.Variant.Color}",
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.DiscountAmount,
                    TotalPrice = (oi.UnitPrice * oi.Quantity) - oi.DiscountAmount
                }),
                Payment = order.Payment != null ? new
                {
                    order.Payment.Id,
                    order.Payment.PaymentMethod,
                    order.Payment.Amount,
                    order.Payment.Status,
                    order.Payment.TransactionId,
                    order.Payment.CreatedAt,
                    order.Payment.CompletedAt
                } : null,
                Shipment = order.Shipment != null ? new
                {
                    order.Shipment.Id,
                    order.Shipment.TrackingNumber,
                    order.Shipment.Carrier,
                    order.Shipment.Status,
                    order.Shipment.ShippedAt,
                    order.Shipment.DeliveredAt,
                    order.Shipment.Notes
                } : null
            });
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order status updated" });
        }
    }
}
