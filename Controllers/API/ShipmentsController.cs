using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongCuGoiYSanPham.Models;

namespace CongCuGoiYSanPham.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShipmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ShipmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult> GetShipment(int orderId)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.OrderId == orderId);

            if (shipment == null)
                return NotFound();

            return Ok(shipment);
        }

        [HttpPut("{orderId}/ship")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ShipOrder(int orderId)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.OrderId == orderId);

            if (shipment == null)
                return NotFound();

            shipment.Status = "Shipped";
            shipment.ShippedAt = DateTime.UtcNow;
            shipment.Order.Status = "Shipping";
            shipment.Order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order shipped successfully" });
        }

        [HttpPut("{orderId}/deliver")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeliverOrder(int orderId)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.OrderId == orderId);

            if (shipment == null)
                return NotFound();

            shipment.Status = "Delivered";
            shipment.DeliveredAt = DateTime.UtcNow;
            shipment.Order.Status = "Delivered";
            shipment.Order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order delivered successfully" });
        }
    }
}
