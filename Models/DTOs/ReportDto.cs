namespace CongCuGoiYSanPham.Models.DTOs
{
    public class SalesReportDto
    {
        public DateTime Date { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalItems { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class InventoryReportDto
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; }
        public string SKU { get; set; }
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public int DaysSinceLastRestock { get; set; }
        public int TotalSold { get; set; }
        public string Status { get; set; }
    }

    public class OrderStatusReportDto
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }
    }

    public class CancellationReturnRateDto
    {
        public int TotalOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int ReturnedOrders { get; set; }
        public int CompletedOrders { get; set; }
        public double CancellationRate { get; set; }
        public double ReturnRate { get; set; }
        public double CombinedRate { get; set; }
        public decimal CancelledAmount { get; set; }
        public decimal ReturnedAmount { get; set; }
        public decimal LostRevenue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
