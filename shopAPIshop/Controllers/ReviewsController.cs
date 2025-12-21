using ECommerceShop.Data;
using System.Linq;
using ECommerceShop.DTOs;
using ECommerceShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ReviewsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductReviews(
            int productId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? rating = null)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null || !product.IsActive)
                    return NotFound(new { Message = "Product not found" });

                var query = _context.ProductReviews
                    .Include(r => r.User)
                    .Where(r => r.ProductId == productId && r.IsApproved);

                if (rating.HasValue)
                {
                    query = query.Where(r => r.Rating == rating.Value);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ProductReviewDTO
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        UserId = r.UserId,
                        UserName = $"{r.User.FirstName} {r.User.LastName}",
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        IsApproved = r.IsApproved
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Message = "Product reviews retrieved successfully",
                    Data = new
                    {
                        Reviews = reviews,
                        TotalCount = totalCount,
                        PageIndex = page,
                        PageSize = pageSize,
                        TotalPages = totalPages,
                        HasPreviousPage = page > 1,
                        HasNextPage = page < totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reviews for product ID {ProductId}", productId);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserReviews(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var query = _context.ProductReviews
                    .Include(r => r.Product)
                    .Where(r => r.UserId == userId);

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ProductReviewDTO
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        UserId = r.UserId,
                        UserName = $"{r.User.FirstName} {r.User.LastName}",
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        IsApproved = r.IsApproved,
                        ProductName = r.Product.Name,
                        ProductImageUrl = r.Product.ImageUrl
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Message = "User reviews retrieved successfully",
                    Data = new
                    {
                        Reviews = reviews,
                        TotalCount = totalCount,
                        PageIndex = page,
                        PageSize = pageSize,
                        TotalPages = totalPages,
                        HasPreviousPage = page > 1,
                        HasNextPage = page < totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user reviews");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] ProductReviewCreateDTO model, [FromQuery] int productId)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid review data", Errors = GetModelStateErrors() });

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var product = await _context.Products.FindAsync(productId);
                if (product == null || !product.IsActive)
                    return NotFound(new { Message = "Product not found" });

                // Check if user already reviewed this product
                var existingReview = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
                
                if (existingReview != null)
                    return BadRequest(new { Message = "You have already reviewed this product" });

                // Check if user has purchased this product
                var hasPurchased = await _context.OrderItems
                    .AnyAsync(oi => oi.Order.UserId == userId && 
                                   oi.ProductId == productId && 
                                   oi.Order.Status == OrderStatus.Delivered);
                
                if (!hasPurchased)
                    return BadRequest(new { Message = "You can only review products you have purchased" });

                var review = new ProductReview
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = true // Auto-approve for simplicity
                };

                _context.ProductReviews.Add(review);
                await _context.SaveChangesAsync();

                var reviewDto = new ProductReviewDTO
                {
                    Id = review.Id,
                    ProductId = review.ProductId,
                    UserId = review.UserId,
                    UserName = $"{review.User.FirstName} {review.User.LastName}",
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    IsApproved = review.IsApproved
                };

                return CreatedAtAction(nameof(GetProductReviews), new { productId = productId }, new
                {
                    Message = "Review created successfully",
                    Data = reviewDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for product ID {ProductId}", productId);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] ProductReviewCreateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid review data", Errors = GetModelStateErrors() });

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var review = await _context.ProductReviews.FindAsync(id);
                if (review == null)
                    return NotFound(new { Message = "Review not found" });

                if (review.UserId != userId)
                    return Forbid();

                review.Rating = model.Rating;
                review.Comment = model.Comment;
                // Note: CreatedAt remains unchanged, IsApproved remains as is

                await _context.SaveChangesAsync();

                var reviewDto = new ProductReviewDTO
                {
                    Id = review.Id,
                    ProductId = review.ProductId,
                    UserId = review.UserId,
                    UserName = $"{review.User.FirstName} {review.User.LastName}",
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    IsApproved = review.IsApproved
                };

                return Ok(new
                {
                    Message = "Review updated successfully",
                    Data = reviewDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review with ID {ReviewId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var review = await _context.ProductReviews.FindAsync(id);
                if (review == null)
                    return NotFound(new { Message = "Review not found" });

                if (review.UserId != userId)
                    return Forbid();

                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review with ID {ReviewId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        // Admin endpoints
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllReviews(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isApproved = null,
            [FromQuery] int? rating = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.ProductReviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .AsQueryable();

                if (isApproved.HasValue)
                {
                    query = query.Where(r => r.IsApproved == isApproved.Value);
                }

                if (rating.HasValue)
                {
                    query = query.Where(r => r.Rating == rating.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(r => 
                        r.Comment.Contains(search) ||
                        r.User.Email.Contains(search) ||
                        r.User.FirstName.Contains(search) ||
                        r.User.LastName.Contains(search) ||
                        r.Product.Name.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ProductReviewDTO
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        UserId = r.UserId,
                        UserName = $"{r.User.FirstName} {r.User.LastName}",
                        UserEmail = r.User.Email,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        IsApproved = r.IsApproved,
                        ProductName = r.Product.Name,
                        ProductImageUrl = r.Product.ImageUrl
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Message = "All reviews retrieved successfully",
                    Data = new
                    {
                        Reviews = reviews,
                        TotalCount = totalCount,
                        PageIndex = page,
                        PageSize = pageSize,
                        TotalPages = totalPages,
                        HasPreviousPage = page > 1,
                        HasNextPage = page < totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all reviews");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPut("admin/{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveReview(int id)
        {
            try
            {
                var review = await _context.ProductReviews.FindAsync(id);
                if (review == null)
                    return NotFound(new { Message = "Review not found" });

                review.IsApproved = true;
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Review approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving review with ID {ReviewId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPut("admin/{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectReview(int id)
        {
            try
            {
                var review = await _context.ProductReviews.FindAsync(id);
                if (review == null)
                    return NotFound(new { Message = "Review not found" });

                review.IsApproved = false;
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Review rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting review with ID {ReviewId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
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