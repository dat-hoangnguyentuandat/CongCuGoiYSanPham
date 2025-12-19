using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongCuGoiYSanPham.Models;
using CongCuGoiYSanPham.Models.DTOs;

namespace CongCuGoiYSanPham.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("sales")]
        public async Task<ActionResult<IEnumerable<SalesReportDto>>> GetSalesReport(
            [FromQuery] string startDate = null,
            [FromQuery] string endDate = null)
        {
            try
            {
                // Parse dates
                DateTime parsedStartDate;
                DateTime parsedEndDate;
                
                if (string.IsNullOrEmpty(startDate))
                {
                    parsedStartDate = DateTime.Now.AddDays(-365);
                }
                else
                {
                    if (!DateTime.TryParse(startDate, out parsedStartDate))
                    {
                        return BadRequest("Invalid start date format");
                    }
                }
                
                if (string.IsNullOrEmpty(endDate))
                {
                    parsedEndDate = DateTime.Now.AddDays(1);
                }
                else
                {
                    if (!DateTime.TryParse(endDate, out parsedEndDate))
                    {
                        return BadRequest("Invalid end date format");
                    }
                    parsedEndDate = parsedEndDate.AddDays(1);
                }

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.CreatedAt >= parsedStartDate && o.CreatedAt <= parsedEndDate && o.Status != "Cancelled")
                    .ToListAsync();

                if (!orders.Any())
                {
                    return Ok(new List<SalesReportDto>());
                }

                var report = orders
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new SalesReportDto
                    {
                        Date = g.Key,
                        TotalRevenue = g.Sum(o => o.TotalAmount),
                        TotalOrders = g.Count(),
                        TotalItems = g.Sum(o => o.OrderItems.Sum(oi => oi.Quantity)),
                        AverageOrderValue = g.Average(o => o.TotalAmount)
                    })
                    .OrderBy(r => r.Date)
                    .ToList();

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating sales report", error = ex.Message });
            }
        }

        [HttpGet("inventory/slow-moving")]
        public async Task<ActionResult<IEnumerable<InventoryReportDto>>> GetSlowMovingInventory()
        {
            try
            {
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);

                var variants = await _context.Variants
                    .Include(v => v.Product)
                    .Include(v => v.Inventory)
                    .Include(v => v.OrderItems)
                        .ThenInclude(oi => oi.Order)
                    .Where(v => v.IsActive)
                    .ToListAsync();

                var report = variants
                    .Select(v => new InventoryReportDto
                    {
                        VariantId = v.Id,
                        ProductName = v.Product.Name,
                        SKU = v.SKU,
                        CurrentStock = v.Inventory?.Quantity ?? 0,
                        ReorderLevel = v.Inventory?.ReorderLevel ?? 0,
                        DaysSinceLastRestock = (DateTime.Now - (v.Inventory?.LastRestockedAt ?? DateTime.Now)).Days,
                        TotalSold = v.OrderItems
                            .Where(oi => oi.Order != null && oi.Order.CreatedAt >= thirtyDaysAgo)
                            .Sum(oi => oi.Quantity),
                        Status = GetInventoryStatus(v)
                    })
                    .Where(r => r.Status == "Slow Moving" || r.Status == "Low Stock")
                    .OrderByDescending(r => r.CurrentStock)
                    .ToList();

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating inventory report", error = ex.Message });
            }
        }

        [HttpGet("orders/status")]
        public async Task<ActionResult<IEnumerable<OrderStatusReportDto>>> GetOrderStatusReport()
        {
            try
            {
                var orders = await _context.Orders.ToListAsync();
                
                if (!orders.Any())
                {
                    return Ok(new List<OrderStatusReportDto>());
                }
                
                var totalOrders = orders.Count;

                var report = orders
                    .GroupBy(o => o.Status)
                    .Select(g => new OrderStatusReportDto
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(o => o.TotalAmount),
                        Percentage = totalOrders > 0 ? (double)g.Count() / totalOrders * 100 : 0
                    })
                    .OrderByDescending(r => r.Count)
                    .ToList();

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating order status report", error = ex.Message });
            }
        }

        [HttpGet("orders/cancellation-return-rate")]
        public async Task<ActionResult<CancellationReturnRateDto>> GetCancellationReturnRate(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var totalOrders = orders.Count;
            var cancelledOrders = orders.Count(o => o.Status == "Cancelled");
            var returnedOrders = orders.Count(o => o.Status == "Returned");
            var completedOrders = orders.Count(o => o.Status == "Delivered");

            var cancelledAmount = orders.Where(o => o.Status == "Cancelled").Sum(o => o.TotalAmount);
            var returnedAmount = orders.Where(o => o.Status == "Returned").Sum(o => o.TotalAmount);

            return Ok(new CancellationReturnRateDto
            {
                TotalOrders = totalOrders,
                CancelledOrders = cancelledOrders,
                ReturnedOrders = returnedOrders,
                CompletedOrders = completedOrders,
                CancellationRate = totalOrders > 0 ? (double)cancelledOrders / totalOrders * 100 : 0,
                ReturnRate = totalOrders > 0 ? (double)returnedOrders / totalOrders * 100 : 0,
                CombinedRate = totalOrders > 0 ? (double)(cancelledOrders + returnedOrders) / totalOrders * 100 : 0,
                CancelledAmount = cancelledAmount,
                ReturnedAmount = returnedAmount,
                LostRevenue = cancelledAmount + returnedAmount,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            });
        }

        private string GetInventoryStatus(Variant variant)
        {
            if (variant.Inventory == null)
                return "No Inventory";

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var recentSales = variant.OrderItems
                .Where(oi => oi.Order != null && oi.Order.CreatedAt >= thirtyDaysAgo)
                .Sum(oi => oi.Quantity);

            if (variant.Inventory.Quantity <= variant.Inventory.ReorderLevel)
                return "Low Stock";

            if (recentSales < 5)
                return "Slow Moving";

            return "Normal";
        }
    }
}
