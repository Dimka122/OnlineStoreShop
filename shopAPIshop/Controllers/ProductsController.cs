using ECommerceShop.DTOs;
using ECommerceShop.Data;
using ECommerceShop.Models;
using ECommerceShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace ECommerceShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] bool? isFeatured = null,
            [FromQuery] bool? onSale = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .Where(p => p.IsActive);

                // Apply filters
                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => 
                        p.Name.Contains(search) || 
                        p.Description.Contains(search) ||
                        p.Category.Name.Contains(search));
                }

                if (isFeatured.HasValue)
                {
                    query = query.Where(p => p.IsFeatured == isFeatured.Value);
                }

                if (onSale.HasValue)
                {
                    if (onSale.Value)
                    {
                        query = query.Where(p => p.SalePrice.HasValue && p.SalePrice > 0);
                    }
                    else
                    {
                        query = query.Where(p => !p.SalePrice.HasValue || p.SalePrice <= 0);
                    }
                }

                // Apply sorting
                sortBy = sortBy?.ToLower() ?? "createdat";
                sortOrder = sortOrder?.ToLower() ?? "desc";

                query = sortOrder == "asc" 
                    ? query.OrderBy($"{sortBy} ASC")
                    : query.OrderBy($"{sortBy} DESC");

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
                            UserName = r.User.FirstName + " " + r.User.LastName,
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
                    Message = "Products retrieved successfully",
                    Data = productList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                    return NotFound(new { Message = "Product not found" });

                var productDto = new ProductDTO
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    SalePrice = product.SalePrice,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    IsFeatured = product.IsFeatured,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    Category = new CategoryDTO
                    {
                        Id = product.Category.Id,
                        Name = product.Category.Name,
                        Description = product.Category.Description,
                        ImageUrl = product.Category.ImageUrl,
                        IsActive = product.Category.IsActive,
                        CreatedAt = product.Category.CreatedAt,
                        ProductCount = _context.Products.Count(p => p.CategoryId == product.CategoryId && p.IsActive)
                    },
                    Reviews = product.Reviews.Select(r => new ProductReviewDTO
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
                };

                return Ok(new
                {
                    Message = "Product retrieved successfully",
                    Data = productDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID {ProductId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}/related")]
        public async Task<IActionResult> GetRelatedProducts(int id, [FromQuery] int limit = 4)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(new { Message = "Product not found" });

                var relatedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .Where(p => p.CategoryId == product.CategoryId && 
                                p.Id != product.Id && 
                                p.IsActive)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(limit)
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

                return Ok(new
                {
                    Message = "Related products retrieved successfully",
                    Data = relatedProducts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving related products for product ID {ProductId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid product data", Errors = GetModelStateErrors() });

                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category == null)
                    return BadRequest(new { Message = "Category not found" });

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    SalePrice = model.SalePrice,
                    StockQuantity = model.StockQuantity,
                    ImageUrl = model.ImageUrl,
                    IsActive = model.IsActive,
                    IsFeatured = model.IsFeatured,
                    CategoryId = model.CategoryId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var productDto = await GetProductDtoAsync(product.Id);

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new
                {
                    Message = "Product created successfully",
                    Data = productDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid product data", Errors = GetModelStateErrors() });

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(new { Message = "Product not found" });

                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category == null)
                    return BadRequest(new { Message = "Category not found" });

                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.SalePrice = model.SalePrice;
                product.StockQuantity = model.StockQuantity;
                product.ImageUrl = model.ImageUrl;
                product.IsActive = model.IsActive;
                product.IsFeatured = model.IsFeatured;
                product.CategoryId = model.CategoryId;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var productDto = await GetProductDtoAsync(product.Id);

                return Ok(new
                {
                    Message = "Product updated successfully",
                    Data = productDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(new { Message = "Product not found" });

                // Soft delete
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Product deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        private async Task<ProductDTO> GetProductDtoAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .ThenInclude(r => r.User)
                .FirstAsync(p => p.Id == productId);

            return new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                SalePrice = product.SalePrice,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                Category = new CategoryDTO
                {
                    Id = product.Category.Id,
                    Name = product.Category.Name,
                    Description = product.Category.Description,
                    ImageUrl = product.Category.ImageUrl,
                    IsActive = product.Category.IsActive,
                    CreatedAt = product.Category.CreatedAt,
                    ProductCount = _context.Products.Count(p => p.CategoryId == product.CategoryId && p.IsActive)
                },
                Reviews = product.Reviews.Select(r => new ProductReviewDTO
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