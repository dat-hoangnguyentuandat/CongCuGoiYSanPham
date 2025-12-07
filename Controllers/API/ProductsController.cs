using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongCuGoiYSanPham.Models;
using CongCuGoiYSanPham.Models.DTOs;

namespace CongCuGoiYSanPham.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
            [FromQuery] string search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants.Where(v => v.IsActive))
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive && p.Variants.Any(v => v.IsActive));

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                // Add price filtering
                if (minPrice.HasValue || maxPrice.HasValue)
                {
                    if (minPrice.HasValue && maxPrice.HasValue)
                    {
                        // Both min and max price provided
                        query = query.Where(p => p.Variants.Any(v => v.IsActive && 
                            (v.DiscountPrice ?? v.Price) >= minPrice.Value && 
                            (v.DiscountPrice ?? v.Price) <= maxPrice.Value));
                    }
                    else if (minPrice.HasValue)
                    {
                        // Only min price provided
                        query = query.Where(p => p.Variants.Any(v => v.IsActive && 
                            (v.DiscountPrice ?? v.Price) >= minPrice.Value));
                    }
                    else if (maxPrice.HasValue)
                    {
                        // Only max price provided
                        query = query.Where(p => p.Variants.Any(v => v.IsActive && 
                            (v.DiscountPrice ?? v.Price) <= maxPrice.Value));
                    }
                }

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category.Name,
                        MinPrice = p.Variants.Where(v => v.IsActive).Min(v => v.DiscountPrice ?? v.Price),
                        MaxPrice = p.Variants.Where(v => v.IsActive).Max(v => v.Price),
                        AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                        ReviewCount = p.Reviews.Count
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading products", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants.Where(v => v.IsActive)).ThenInclude(v => v.Inventory)
                    .Include(p => p.Reviews).ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                    return NotFound(new { message = "Product not found" });

                var activeVariants = product.Variants.Where(v => v.IsActive).ToList();
                if (!activeVariants.Any())
                    return NotFound(new { message = "Product has no active variants" });

                var dto = new ProductDetailDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    ImageUrl = product.ImageUrl,
                    CategoryName = product.Category.Name,
                    MinPrice = activeVariants.Min(v => v.DiscountPrice ?? v.Price),
                    MaxPrice = activeVariants.Max(v => v.Price),
                    AverageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = product.Reviews.Count,
                    Variants = activeVariants.Select(v => new VariantDto
                    {
                        Id = v.Id,
                        SKU = v.SKU,
                        Size = v.Size,
                        Color = v.Color,
                        Price = v.Price,
                        DiscountPrice = v.DiscountPrice,
                        StockQuantity = v.Inventory?.Quantity ?? 0,
                        IsActive = v.IsActive
                    }).ToList(),
                    Reviews = product.Reviews.Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        UserName = r.User.FullName,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        IsVerifiedPurchase = r.IsVerifiedPurchase,
                        CreatedAt = r.CreatedAt
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading product", error = ex.Message });
            }
        }

        [HttpGet("variants/{id}")]
        public async Task<ActionResult<object>> GetVariant(int id)
        {
            try
            {
                var variant = await _context.Variants
                    .Include(v => v.Product)
                    .Include(v => v.Inventory)
                    .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);

                if (variant == null)
                    return NotFound(new { message = "Variant not found" });

                var result = new
                {
                    id = variant.Id,
                    sku = variant.SKU,
                    size = variant.Size,
                    color = variant.Color,
                    price = variant.Price,
                    discountPrice = variant.DiscountPrice,
                    stockQuantity = variant.Inventory?.Quantity ?? 0,
                    isActive = variant.IsActive,
                    product = new
                    {
                        id = variant.Product.Id,
                        name = variant.Product.Name,
                        description = variant.Product.Description,
                        imageUrl = variant.Product.ImageUrl
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading variant", error = ex.Message });
            }
        }
    }
}
