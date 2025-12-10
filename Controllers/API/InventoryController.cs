using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongCuGoiYSanPham.Models;

namespace CongCuGoiYSanPham.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class InventoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetInventory()
        {
            var inventory = await _context.Inventories
                .Include(i => i.Variant).ThenInclude(v => v.Product)
                .Select(i => new
                {
                    i.Id,
                    i.VariantId,
                    ProductName = i.Variant.Product.Name,
                    SKU = i.Variant.SKU,
                    i.Quantity,
                    i.ReorderLevel,
                    i.LastRestockedAt,
                    Status = i.Quantity <= i.ReorderLevel ? "Low Stock" : "In Stock"
                })
                .ToListAsync();

            return Ok(inventory);
        }

        [HttpPut("{variantId}/restock")]
        public async Task<ActionResult> RestockInventory(int variantId, [FromBody] int quantity)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.VariantId == variantId);

            if (inventory == null)
                return NotFound();

            inventory.Quantity += quantity;
            inventory.LastRestockedAt = DateTime.UtcNow;
            inventory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Inventory restocked", newQuantity = inventory.Quantity });
        }
    }
}
