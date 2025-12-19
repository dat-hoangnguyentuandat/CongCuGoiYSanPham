using CongCuGoiYSanPham.Models;

namespace CongCuGoiYSanPham.Services
{
    public interface IProductRecommendationService
    {
        Task<List<Product>> GetRecommendedProductsAsync(string query, int userId = 0, int limit = 10);
        Task<List<Product>> GetSimilarProductsAsync(int productId, int limit = 5);
        Task<List<Product>> GetPersonalizedRecommendationsAsync(string userId, int limit = 10);
        Task<string> GetAIProductSuggestionsAsync(string userQuery, List<Product> products, List<Category> categories);
    }
}