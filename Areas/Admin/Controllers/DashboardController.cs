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
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetDashboardStats()
        {
            var today = DateTime.UtcNow.Date;
            var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // Total stats
            var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
            var totalOrders = await _context.Orders.CountAsync();
            var totalCustomers = await _context.Users.CountAsync();
            var totalRevenue = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            // Today stats
            var todayOrders = await _context.Orders
                .Where(o => o.CreatedAt.Date == today)
                .CountAsync();
            var todayRevenue = await _context.Orders
                .Where(o => o.CreatedAt.Date == today && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // This month stats
            var monthOrders = await _context.Orders
                .Where(o => o.CreatedAt >= thisMonth)
                .CountAsync();
            var monthRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= thisMonth && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Pending orders
            var pendingOrders = await _context.Orders
                .Where(o => o.Status == "Pending" || o.Status == "Confirmed")
                .CountAsync();

            // Low stock products
            var lowStockProducts = await _context.Inventories
                .Include(i => i.Variant).ThenInclude(v => v.Product)
                .Where(i => i.Quantity <= i.ReorderLevel)
                .CountAsync();

            // Recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    CustomerName = o.User.FullName,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt
                })
                .ToListAsync();

            // Top selling products
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Variant).ThenInclude(v => v.Product)
                .Where(oi => oi.Order.Status != "Cancelled")
                .GroupBy(oi => new { oi.Variant.Product.Id, oi.Variant.Product.Name })
                .Select(g => new
                {
                    ProductId = g.Key.Id,
                    ProductName = g.Key.Name,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                overview = new
                {
                    totalProducts,
                    totalOrders,
                    totalCustomers,
                    totalRevenue,
                    pendingOrders,
                    lowStockProducts
                },
                today = new
                {
                    orders = todayOrders,
                    revenue = todayRevenue
                },
                thisMonth = new
                {
                    orders = monthOrders,
                    revenue = monthRevenue
                },
                recentOrders,
                topProducts
            });
        }

        [HttpGet("revenue-chart")]
        public async Task<ActionResult> GetRevenueChart([FromQuery] int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;

            var data = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.Status != "Cancelled")
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("order-status-chart")]
        public async Task<ActionResult> GetOrderStatusChart()
        {
            var data = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / _context.Orders.Count() * 100
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}
