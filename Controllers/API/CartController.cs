using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongCuGoiYSanPham.Models;
using CongCuGoiYSanPham.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;

namespace CongCuGoiYSanPham.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Sử dụng default authentication (Cookie)
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var dto = new CartDto
            {
                Id = cart.Id,
                Items = cart.CartItems.Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    VariantId = ci.VariantId,
                    ProductName = ci.Variant.Product.Name,
                    VariantInfo = $"{ci.Variant.Size} - {ci.Variant.Color}",
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Variant.DiscountPrice ?? ci.Variant.Price,
                    TotalPrice = (ci.Variant.DiscountPrice ?? ci.Variant.Price) * ci.Quantity,
                    ImageUrl = ci.Variant.Product.ImageUrl
                }).ToList(),
                TotalAmount = cart.CartItems.Sum(ci => (ci.Variant.DiscountPrice ?? ci.Variant.Price) * ci.Quantity),
                TotalItems = cart.CartItems.Sum(ci => ci.Quantity)
            };

            return Ok(dto);
        }

        [HttpPost("items")]
        public async Task<ActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var userId = GetUserId();
                
                // Check variant and inventory
                var variant = await _context.Variants
                    .Include(v => v.Inventory)
                    .FirstOrDefaultAsync(v => v.Id == dto.VariantId && v.IsActive);

                if (variant == null)
                    return BadRequest(new { message = "Sản phẩm không tồn tại" });

                if (variant.Inventory == null || variant.Inventory.Quantity < dto.Quantity)
                    return BadRequest(new { message = $"Chỉ còn {variant.Inventory?.Quantity ?? 0} sản phẩm trong kho" });

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                }

                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.VariantId == dto.VariantId);
                int totalQuantity = dto.Quantity;
                
                if (existingItem != null)
                {
                    totalQuantity += existingItem.Quantity;
                }

                // Check total quantity against inventory
                if (totalQuantity > variant.Inventory.Quantity)
                {
                    return BadRequest(new { message = $"Tổng số lượng vượt quá tồn kho. Chỉ còn {variant.Inventory.Quantity} sản phẩm" });
                }

                if (existingItem != null)
                {
                    existingItem.Quantity = totalQuantity;
                }
                else
                {
                    cart.CartItems.Add(new CartItem
                    {
                        VariantId = dto.VariantId,
                        Quantity = dto.Quantity
                    });
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã thêm vào giỏ hàng" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPut("items/{id}")]
        public async Task<ActionResult> UpdateCartItem(int id, [FromBody] int quantity)
        {
            try
            {
                var userId = GetUserId();
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .Include(ci => ci.Variant).ThenInclude(v => v.Inventory)
                    .FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);

                if (cartItem == null)
                    return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng" });

                // Check inventory
                if (cartItem.Variant.Inventory == null || quantity > cartItem.Variant.Inventory.Quantity)
                {
                    return BadRequest(new { message = $"Chỉ còn {cartItem.Variant.Inventory?.Quantity ?? 0} sản phẩm trong kho" });
                }

                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = quantity;
                }

                cartItem.Cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã cập nhật giỏ hàng" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpDelete("items/{id}")]
        public async Task<ActionResult> RemoveFromCart(int id)
        {
            var userId = GetUserId();
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.UserId == userId);

            if (cartItem == null)
                return NotFound();

            _context.CartItems.Remove(cartItem);
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item removed from cart" });
        }

        [HttpDelete]
        public async Task<ActionResult> ClearCart()
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null)
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Cart cleared" });
        }
    }
}
