using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongCuGoiYSanPham.Models;
using CongCuGoiYSanPham.Models.DTOs;
using CongCuGoiYSanPham.Services;
using System.Security.Claims;

namespace CongCuGoiYSanPham.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductRecommendationService _recommendationService;

        public AIController(ApplicationDbContext context, IProductRecommendationService recommendationService)
        {
            _context = context;
            _recommendationService = recommendationService;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<object>> GetAIRecommendations([FromBody] AIQueryRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Query))
                    return BadRequest("Query is required");

                // Lấy danh sách sản phẩm và danh mục
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants.Where(v => v.IsActive))
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive)
                    .Take(50) // Giới hạn để tránh prompt quá dài
                    .ToListAsync();

                var categories = await _context.Categories
                    .Take(20)
                    .ToListAsync();

                // Gọi AI service để lấy gợi ý
                var aiResponse = await _recommendationService.GetAIProductSuggestionsAsync(request.Query, products, categories);

                // Tìm sản phẩm liên quan để trả về cùng với AI response
                var relatedProducts = await _recommendationService.GetRecommendedProductsAsync(request.Query, 0, 6);

                var productDtos = relatedProducts.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    CategoryName = p.Category?.Name,
                    MinPrice = p.Variants.Where(v => v.IsActive).Any() ? p.Variants.Where(v => v.IsActive).Min(v => v.DiscountPrice ?? v.Price) : 0,
                    MaxPrice = p.Variants.Where(v => v.IsActive).Any() ? p.Variants.Where(v => v.IsActive).Max(v => v.Price) : 0,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews.Count
                }).ToList();

                return Ok(new
                {
                    aiResponse = aiResponse,
                    relatedProducts = productDtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting AI recommendations", error = ex.Message });
            }
        }

        [HttpGet("suggest")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetSuggestions([FromQuery] int? productId = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                List<ProductDto> suggestions;

                if (productId.HasValue)
                {
                    // Suggest similar products based on category
                    var product = await _context.Products.FindAsync(productId.Value);
                    if (product == null)
                        return NotFound();

                    suggestions = await _context.Products
                        .Include(p => p.Category)
                        .Include(p => p.Variants.Where(v => v.IsActive))
                        .Include(p => p.Reviews)
                        .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != productId.Value && p.Variants.Any(v => v.IsActive))
                        .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating))
                        .Take(6)
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
                }
                else if (!string.IsNullOrEmpty(userId))
                {
                    // Personalized suggestions based on user's order history
                    var userCategories = await _context.Orders
                        .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                        .Where(o => o.UserId == userId)
                        .SelectMany(o => o.OrderItems.Select(oi => oi.Variant.Product.CategoryId))
                        .Distinct()
                        .ToListAsync();

                    if (userCategories.Any())
                    {
                        suggestions = await _context.Products
                            .Include(p => p.Category)
                            .Include(p => p.Variants.Where(v => v.IsActive))
                            .Include(p => p.Reviews)
                            .Where(p => p.IsActive && userCategories.Contains(p.CategoryId) && p.Variants.Any(v => v.IsActive))
                            .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating))
                            .Take(8)
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
                    }
                    else
                    {
                        // New user - suggest popular products
                        suggestions = await GetPopularProducts();
                    }
                }
                else
                {
                    // Anonymous user - suggest popular products
                    suggestions = await GetPopularProducts();
                }

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading suggestions", error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> SemanticSearch([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                    return BadRequest("Query is required");

                // Sử dụng service mới để tìm kiếm
                var products = await _recommendationService.GetRecommendedProductsAsync(query, 0, 20);

                var results = products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    CategoryName = p.Category?.Name,
                    MinPrice = p.Variants.Where(v => v.IsActive).Any() ? p.Variants.Where(v => v.IsActive).Min(v => v.DiscountPrice ?? v.Price) : 0,
                    MaxPrice = p.Variants.Where(v => v.IsActive).Any() ? p.Variants.Where(v => v.IsActive).Max(v => v.Price) : 0,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews.Count
                }).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error performing search", error = ex.Message });
            }
        }

        private async Task<List<ProductDto>> GetPopularProducts()
        {
            try
            {
                return await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants.Where(v => v.IsActive))
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive && p.Variants.Any(v => v.IsActive))
                    .OrderByDescending(p => p.Reviews.Count)
                    .ThenByDescending(p => p.Reviews.Average(r => (double?)r.Rating))
                    .Take(8)
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
            }
            catch (Exception)
            {
                // Return empty list if error occurs
                return new List<ProductDto>();
            }
        }

        private int CalculateRelevanceScore(Product product, string[] keywords)
        {
            int score = 0;
            var productText = $"{product.Name} {product.Description} {product.Category.Name}".ToLower();

            foreach (var keyword in keywords)
            {
                if (product.Name.ToLower().Contains(keyword))
                    score += 10;
                if (product.Description?.ToLower().Contains(keyword) == true)
                    score += 5;
                if (product.Category.Name.ToLower().Contains(keyword))
                    score += 3;
            }

            return score;
        }
    }

    public class AIQueryRequest
    {
        public string Query { get; set; }
    }
}
