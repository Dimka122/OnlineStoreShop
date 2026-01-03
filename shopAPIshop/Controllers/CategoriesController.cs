using ECommerceShop.DTOs;
using ECommerceShop.Models;
using ECommerceShop.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace ECommerceShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ApplicationDbContext context,
            ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Include(c => c.Products.Where(p => p.IsActive))
                    .Select(c => new CategoryDTO
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        IsActive = c.IsActive,
                        CreatedAt = c.CreatedAt,
                        ProductCount = c.Products.Count
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Ok(new
                {
                    Message = "Categories retrieved successfully",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products.Where(p => p.IsActive))
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (category == null)
                    return NotFound(new { Message = "Category not found" });

                var categoryDto = new CategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    ImageUrl = category.ImageUrl,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    ProductCount = category.Products.Count
                };

                return Ok(new
                {
                    Message = "Category retrieved successfully",
                    Data = categoryDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}/products")]
        public async Task<IActionResult> GetCategoryProducts(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null || !category.IsActive)
                    return NotFound(new { Message = "Category not found" });

                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .Where(p => p.CategoryId == id && p.IsActive);

                // Apply sorting
                sortBy = sortBy?.ToLower() ?? "createdat";
                sortOrder = sortOrder?.ToLower() ?? "desc";

                if (sortOrder == "asc")
                {
                    query = query.OrderBy(sortBy);
                }
                else
                {
                    query = query.OrderBy($"{sortBy} descending");
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        SalePrice = p.SalePrice,
                        StockQuantity = p.StockQuantity,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        Category = new CategoryDTO
                        {
                            Id = p.Category.Id,
                            Name = p.Category.Name,
                            Description = p.Category.Description,
                            ImageUrl = p.Category.ImageUrl,
                            IsActive = p.Category.IsActive,
                            CreatedAt = p.Category.CreatedAt,
                            ProductCount = 0
                        },
                        Reviews = p.Reviews.Select(r => new ProductReviewDTO
                        {
                            Id = r.Id,
                            ProductId = r.ProductId,
                            UserId = r.UserId,
                            UserName = $"{r.User.FirstName} {r.User.LastName}",
                            Rating = r.Rating,
                            Comment = r.Comment,
                            CreatedAt = r.CreatedAt,
                            IsApproved = r.IsApproved
                        }).ToList()
                    })
                    .ToListAsync();

                var productList = new ProductListDTO
                {
                    Products = products,
                    TotalCount = totalCount,
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                };

                return Ok(new
                {
                    Message = "Category products retrieved successfully",
                    Data = productList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for category ID {CategoryId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid category data", Errors = GetModelStateErrors() });

                // Check if category name already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower());
                
                if (existingCategory != null)
                    return BadRequest(new { Message = "Category with this name already exists" });

                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description,
                    ImageUrl = model.ImageUrl,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                var categoryDto = new CategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    ImageUrl = category.ImageUrl,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    ProductCount = 0
                };

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new
                {
                    Message = "Category created successfully",
                    Data = categoryDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryCreateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid category data", Errors = GetModelStateErrors() });

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound(new { Message = "Category not found" });

                // Check if category name already exists (excluding current category)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower() && c.Id != id);
                
                if (existingCategory != null)
                    return BadRequest(new { Message = "Category with this name already exists" });

                category.Name = model.Name;
                category.Description = model.Description;
                category.ImageUrl = model.ImageUrl;
                category.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                var productCount = await _context.Products.CountAsync(p => p.CategoryId == id && p.IsActive);

                var categoryDto = new CategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    ImageUrl = category.ImageUrl,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    ProductCount = productCount
                };

                return Ok(new
                {
                    Message = "Category updated successfully",
                    Data = categoryDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID {CategoryId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

       // [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound(new { Message = "Category not found" });

                if (category.Products.Any(p => p.IsActive))
                    return BadRequest(new { Message = "Cannot delete category with active products" });

                // Soft delete
                category.IsActive = false;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
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