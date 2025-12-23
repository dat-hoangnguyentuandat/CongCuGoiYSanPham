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
    public class PromotionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PromotionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllPromotions()
        {
            var promotions = await _context.Promotions
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
            return Ok(promotions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetPromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return NotFound();

            return Ok(promotion);
        }

        [HttpPost]
        public async Task<ActionResult> CreatePromotion([FromBody] PromotionDto dto)
        {
            var promotion = new Promotion
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinOrderAmount = dto.MinOrderAmount,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                UsageLimit = dto.UsageLimit,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = true
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, promotion);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePromotion(int id, [FromBody] PromotionDto dto)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return NotFound();

            promotion.Code = dto.Code;
            promotion.Name = dto.Name;
            promotion.Description = dto.Description;
            promotion.DiscountType = dto.DiscountType;
            promotion.DiscountValue = dto.DiscountValue;
            promotion.MinOrderAmount = dto.MinOrderAmount;
            promotion.MaxDiscountAmount = dto.MaxDiscountAmount;
            promotion.UsageLimit = dto.UsageLimit;
            promotion.StartDate = dto.StartDate;
            promotion.EndDate = dto.EndDate;

            await _context.SaveChangesAsync();

            return Ok(promotion);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return NotFound();

            promotion.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Promotion deactivated" });
        }

        [HttpPut("{id}/toggle")]
        public async Task<ActionResult> TogglePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return NotFound();

            promotion.IsActive = !promotion.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Promotion {(promotion.IsActive ? "activated" : "deactivated")}" });
        }
    }

    public class PromotionDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
