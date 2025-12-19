using CongCuGoiYSanPham.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CongCuGoiYSanPham.Services
{
    public class ProductRecommendationService : IProductRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _geminiApiKey;
        private readonly string _geminiApiUrl;
        private readonly ILogger<ProductRecommendationService> _logger;

        public ProductRecommendationService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ProductRecommendationService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _geminiApiKey = configuration["Gemini:ApiKey"];
            _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";
            _logger = logger;
        }

        public async Task<List<Product>> GetRecommendedProductsAsync(string query, int userId = 0, int limit = 10)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive)
                    .ToListAsync();

                if (string.IsNullOrEmpty(query))
                {
                    // Trả về sản phẩm phổ biến nếu không có query
                    return products
                        .OrderByDescending(p => p.Reviews.Count)
                        .ThenByDescending(p => p.CreatedAt)
                        .Take(limit)
                        .ToList();
                }

                // Tìm kiếm theo tên và mô tả
                var filteredProducts = products
                    .Where(p => 
                        p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        (p.Description != null && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        p.Category.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(p => p.Reviews.Count)
                    .Take(limit)
                    .ToList();

                return filteredProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended products for query: {Query}", query);
                return new List<Product>();
            }
        }

        public async Task<List<Product>> GetSimilarProductsAsync(int productId, int limit = 5)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                    return new List<Product>();

                // Tìm sản phẩm cùng danh mục
                var similarProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .Where(p => p.CategoryId == product.CategoryId && p.Id != productId && p.IsActive)
                    .OrderByDescending(p => p.Reviews.Count)
                    .Take(limit)
                    .ToListAsync();

                return similarProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar products for product ID: {ProductId}", productId);
                return new List<Product>();
            }
        }

        public async Task<List<Product>> GetPersonalizedRecommendationsAsync(string userId, int limit = 10)
        {
            try
            {
                // Lấy lịch sử đơn hàng của user
                var userOrders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.Category)
                    .Where(o => o.UserId == userId)
                    .ToListAsync();

                if (!userOrders.Any())
                {
                    // Nếu user chưa có đơn hàng, trả về sản phẩm phổ biến
                    return await GetRecommendedProductsAsync("", 0, limit);
                }

                // Lấy danh mục sản phẩm user đã mua
                var purchasedCategoryIds = userOrders
                    .SelectMany(o => o.OrderItems)
                    .Select(oi => oi.Variant.Product.CategoryId)
                    .Distinct()
                    .ToList();

                // Gợi ý sản phẩm từ các danh mục user quan tâm
                var recommendedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .Where(p => purchasedCategoryIds.Contains(p.CategoryId) && p.IsActive)
                    .OrderByDescending(p => p.Reviews.Count)
                    .Take(limit)
                    .ToListAsync();

                return recommendedProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations for user: {UserId}", userId);
                return new List<Product>();
            }
        }

        public async Task<string> GetAIProductSuggestionsAsync(string userQuery, List<Product> products, List<Category> categories)
        {
            try
            {
                if (string.IsNullOrEmpty(_geminiApiKey))
                {
                    _logger.LogError("Gemini API key is not configured");
                    return "Xin lỗi, hiện tại hệ thống AI đang gặp vấn đề kỹ thuật. Vui lòng thử lại sau.";
                }

                var prompt = CreateProductRecommendationPrompt(userQuery, products, categories);
                
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync(_geminiApiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API request failed with status {StatusCode}: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    return "Xin lỗi, hiện tại hệ thống AI đang gặp vấn đề kỹ thuật. Vui lòng thử lại sau.";
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (!responseObject.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Gemini API response does not contain candidates");
                    return "Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này.";
                }
                
                var answer = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                
                return answer ?? "Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling Gemini API for product suggestions");
                return "Xin lỗi, hiện tại hệ thống AI đang gặp vấn đề kỹ thuật. Vui lòng thử lại sau.";
            }
        }

        private string CreateProductRecommendationPrompt(string userQuery, List<Product> products, List<Category> categories)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("Bạn là một chuyên gia tư vấn sản phẩm thông minh của hệ thống thương mại điện tử. Nhiệm vụ của bạn là gợi ý sản phẩm phù hợp nhất cho khách hàng dựa trên yêu cầu của họ.");
            prompt.AppendLine();
            
            prompt.AppendLine("Hướng dẫn phản hồi:");
            prompt.AppendLine("1. Luôn trả lời bằng tiếng Việt");
            prompt.AppendLine("2. Phân tích yêu cầu của khách hàng và tìm sản phẩm phù hợp trong danh sách được cung cấp");
            prompt.AppendLine("3. Ưu tiên gợi ý 3-5 sản phẩm phù hợp nhất");
            prompt.AppendLine("4. Trình bày thông tin sản phẩm theo định dạng:");
            prompt.AppendLine("   - Tên sản phẩm: [Tên]");
            prompt.AppendLine("   - Mô tả: [Mô tả ngắn gọn]");
            prompt.AppendLine("   - Danh mục: [Tên danh mục]");
            prompt.AppendLine("   - Lý do gợi ý: [Tại sao phù hợp với yêu cầu]");
            prompt.AppendLine("5. Nếu không tìm thấy sản phẩm phù hợp, gợi ý sản phẩm tương tự hoặc giải thích lý do");
            prompt.AppendLine("6. Kết thúc bằng lời mời khách hàng đặt câu hỏi thêm");
            prompt.AppendLine();
            
            prompt.AppendLine("Danh sách danh mục sản phẩm:");
            foreach (var category in categories.Take(10))
            {
                prompt.AppendLine($"- {category.Name}: {category.Description}");
            }
            prompt.AppendLine();
            
            prompt.AppendLine("Danh sách sản phẩm có sẵn:");
            foreach (var product in products.Take(20))
            {
                prompt.AppendLine($"- Tên: {product.Name}");
                prompt.AppendLine($"  Mô tả: {product.Description ?? "Không có mô tả"}");
                prompt.AppendLine($"  Danh mục: {product.Category?.Name ?? "Không xác định"}");
                prompt.AppendLine($"  Số đánh giá: {product.Reviews?.Count ?? 0}");
                prompt.AppendLine();
            }
            
            prompt.AppendLine($"Yêu cầu của khách hàng: {userQuery}");
            prompt.AppendLine();
            prompt.AppendLine("Hãy phân tích yêu cầu và gợi ý những sản phẩm phù hợp nhất từ danh sách trên.");
            
            return prompt.ToString();
        }
    }
}