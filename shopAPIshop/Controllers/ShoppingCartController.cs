using ECommerceShop.DTOs;
using ECommerceShop.Models;
using ECommerceShop.Data;
using Microsoft.Extensions.Logging;
using ECommerceShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShoppingCartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ShoppingCartController> _logger;

        public ShoppingCartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ShoppingCartController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetShoppingCart()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var shoppingCart = await _context.ShoppingCarts
                    .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(sc => sc.UserId == userId);

                if (shoppingCart == null)
                {
                    shoppingCart = new ShoppingCart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        CartItems = new List<CartItem>()
                    };
                    _context.ShoppingCarts.Add(shoppingCart);
                    await _context.SaveChangesAsync();
                }

                var cartDto = new ShoppingCartDTO
                {
                    Id = shoppingCart.Id,
                    UserId = shoppingCart.UserId,
                    CreatedAt = shoppingCart.CreatedAt,
                    CartItems = shoppingCart.CartItems.Select(ci => new CartItemDTO
                    {
                        Id = ci.Id,
                        ProductId = ci.ProductId,
                        ProductName = ci.Product.Name,
                        ProductImageUrl = ci.Product.ImageUrl,
                        CategoryName = ci.Product.Category.Name,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.Product.Price,
                        SalePrice = ci.Product.SalePrice ?? 0,
                        InStock = ci.Product.StockQuantity > 0,
                        AvailableStock = ci.Product.StockQuantity
                    }).ToList()
                };

                return Ok(new
                {
                    Message = "Shopping cart retrieved successfully",
                    Data = cartDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shopping cart");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid cart data", Errors = GetModelStateErrors() });

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null || !product.IsActive)
                    return BadRequest(new { Message = "Product not found" });

                if (product.StockQuantity < model.Quantity)
                    return BadRequest(new { Message = $"Insufficient stock. Only {product.StockQuantity} items available" });

                var shoppingCart = await _context.ShoppingCarts
                    .Include(sc => sc.CartItems)
                    .FirstOrDefaultAsync(sc => sc.UserId == userId);

                if (shoppingCart == null)
                {
                    shoppingCart = new ShoppingCart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        CartItems = new List<CartItem>()
                    };
                    _context.ShoppingCarts.Add(shoppingCart);
                    await _context.SaveChangesAsync();
                }

                var existingCartItem = shoppingCart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ProductId);
                if (existingCartItem != null)
                {
                    var newQuantity = existingCartItem.Quantity + model.Quantity;
                    if (product.StockQuantity < newQuantity)
                        return BadRequest(new { Message = $"Insufficient stock. Only {product.StockQuantity} items available" });

                    existingCartItem.Quantity = newQuantity;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        ProductId = model.ProductId,
                        Quantity = model.Quantity,
                        ShoppingCartId = shoppingCart.Id,
                        AddedAt = DateTime.UtcNow
                    };
                    shoppingCart.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                var cartDto = await GetCartDtoAsync(shoppingCart.Id);

                return Ok(new
                {
                    Message = "Product added to cart successfully",
                    Data = cartDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPut("items/{itemId}")]
        public async Task<IActionResult> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid cart data", Errors = GetModelStateErrors() });

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.ShoppingCart)
                    .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.ShoppingCart.UserId == userId);

                if (cartItem == null)
                    return NotFound(new { Message = "Cart item not found" });

                if (model.Quantity == 0)
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();

                    var cartDto = await GetCartDtoAsync(cartItem.ShoppingCartId);
                    return Ok(new
                    {
                        Message = "Item removed from cart successfully",
                        Data = cartDto
                    });
                }

                if (cartItem.Product.StockQuantity < model.Quantity)
                    return BadRequest(new { Message = $"Insufficient stock. Only {cartItem.Product.StockQuantity} items available" });

                cartItem.Quantity = model.Quantity;
                await _context.SaveChangesAsync();

                var updatedCartDto = await GetCartDtoAsync(cartItem.ShoppingCartId);

                return Ok(new
                {
                    Message = "Cart item updated successfully",
                    Data = updatedCartDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item with ID {ItemId}", itemId);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> RemoveFromCart(int itemId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var cartItem = await _context.CartItems
                    .Include(ci => ci.ShoppingCart)
                    .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.ShoppingCart.UserId == userId);

                if (cartItem == null)
                    return NotFound(new { Message = "Cart item not found" });

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                var cartDto = await GetCartDtoAsync(cartItem.ShoppingCartId);

                return Ok(new
                {
                    Message = "Item removed from cart successfully",
                    Data = cartDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item with ID {ItemId}", itemId);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var shoppingCart = await _context.ShoppingCarts
                    .Include(sc => sc.CartItems)
                    .FirstOrDefaultAsync(sc => sc.UserId == userId);

                if (shoppingCart == null)
                    return NotFound(new { Message = "Shopping cart not found" });

                _context.CartItems.RemoveRange(shoppingCart.CartItems);
                await _context.SaveChangesAsync();

                var cartDto = new ShoppingCartDTO
                {
                    Id = shoppingCart.Id,
                    UserId = shoppingCart.UserId,
                    CreatedAt = shoppingCart.CreatedAt,
                    CartItems = new List<CartItemDTO>()
                };

                return Ok(new
                {
                    Message = "Cart cleared successfully",
                    Data = cartDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing shopping cart");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        private async Task<ShoppingCartDTO> GetCartDtoAsync(int shoppingCartId)
        {
            var shoppingCart = await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.Category)
                .FirstAsync(sc => sc.Id == shoppingCartId);

            return new ShoppingCartDTO
            {
                Id = shoppingCart.Id,
                UserId = shoppingCart.UserId,
                CreatedAt = shoppingCart.CreatedAt,
                CartItems = shoppingCart.CartItems.Select(ci => new CartItemDTO
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    ProductImageUrl = ci.Product.ImageUrl,
                    CategoryName = ci.Product.Category.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    SalePrice = ci.Product.SalePrice ?? 0,
                    InStock = ci.Product.StockQuantity > 0,
                    AvailableStock = ci.Product.StockQuantity
                }).ToList()
            };
        }

        private List<string> GetModelStateErrors()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
        }
    }
}