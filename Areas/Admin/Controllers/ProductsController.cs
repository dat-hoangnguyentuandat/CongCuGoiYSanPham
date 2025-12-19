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
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllProducts([FromQuery] bool includeInactive = false)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            // Mặc định chỉ hiển thị sản phẩm active
            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            var products = await query
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.ImageUrl,
                    CategoryName = p.Category.Name,
                    p.IsActive,
                    VariantCount = p.Variants.Count(v => v.IsActive),
                    p.CreatedAt
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult> CreateProduct([FromBody] ProductCreateDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                IsActive = true
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAllProducts), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateProduct(int id, [FromBody] ProductCreateDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.ImageUrl = dto.ImageUrl;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return NotFound(new { message = "Product not found" });

                // Soft delete: Set IsActive = false
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                // Also deactivate all variants
                foreach (var variant in product.Variants)
                {
                    variant.IsActive = false;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Product deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting product", error = ex.Message });
            }
        }

        [HttpPut("{id}/restore")]
        public async Task<ActionResult> RestoreProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return NotFound(new { message = "Product not found" });

                // Restore product
                product.IsActive = true;
                product.UpdatedAt = DateTime.UtcNow;

                // Also restore all variants
                foreach (var variant in product.Variants)
                {
                    variant.IsActive = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Product restored successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error restoring product", error = ex.Message });
            }
        }

        [HttpPost("{productId}/variants")]
        public async Task<ActionResult> CreateVariant(int productId, [FromBody] VariantCreateDto dto)
        {
            var variant = new Variant
            {
                ProductId = productId,
                SKU = dto.SKU,
                Size = dto.Size,
                Color = dto.Color,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                IsActive = true
            };

            _context.Variants.Add(variant);
            await _context.SaveChangesAsync();

            // Create inventory
            _context.Inventories.Add(new Inventory
            {
                VariantId = variant.Id,
                Quantity = dto.InitialStock,
                ReorderLevel = 10
            });
            await _context.SaveChangesAsync();

            return Ok(variant);
        }

        [HttpPut("variants/{id}")]
        public async Task<ActionResult> UpdateVariant(int id, [FromBody] VariantCreateDto dto)
        {
            var variant = await _context.Variants.FindAsync(id);
            if (variant == null)
                return NotFound();

            variant.SKU = dto.SKU;
            variant.Size = dto.Size;
            variant.Color = dto.Color;
            variant.Price = dto.Price;
            variant.DiscountPrice = dto.DiscountPrice;

            await _context.SaveChangesAsync();

            return Ok(variant);
        }
    }

    public class ProductCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int CategoryId { get; set; }
    }

    public class VariantCreateDto
    {
        public string SKU { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int InitialStock { get; set; }
    }
}
