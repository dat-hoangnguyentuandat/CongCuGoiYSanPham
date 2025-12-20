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
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .ToListAsync();

                var result = categories.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory?.Name,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.Id),
                    SubCategoryCount = _context.Categories.Count(sc => sc.ParentCategoryId == c.Id)
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading categories", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound(new { message = "Category not found" });

                var subCategoryCount = await _context.Categories.CountAsync(sc => sc.ParentCategoryId == id);

                return Ok(new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.ParentCategoryId,
                    ParentCategoryName = category.ParentCategory?.Name,
                    SubCategoryCount = subCategoryCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading category", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateCategory([FromBody] CategoryDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Category name is required" });
                }

                var category = new Category
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    ParentCategoryId = dto.ParentCategoryId
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.ParentCategoryId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating category", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Category name is required" });
                }

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound(new { message = "Category not found" });

                category.Name = dto.Name;
                category.Description = dto.Description;
                category.ParentCategoryId = dto.ParentCategoryId;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.ParentCategoryId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                    return NotFound(new { message = "Category not found" });

                var productCount = await _context.Products.CountAsync(p => p.CategoryId == id);
                var subCategoryCount = await _context.Categories.CountAsync(sc => sc.ParentCategoryId == id);

                if (productCount > 0)
                    return BadRequest(new { message = "Cannot delete category with products" });

                if (subCategoryCount > 0)
                    return BadRequest(new { message = "Cannot delete category with subcategories" });

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting category", error = ex.Message });
            }
        }
    }

    public class CategoryDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int? ParentCategoryId { get; set; }
    }
}
