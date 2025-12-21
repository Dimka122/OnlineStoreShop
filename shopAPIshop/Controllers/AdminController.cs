using ECommerceShop.DTOs;
using ECommerceShop.Models;
using ECommerceShop.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceShop.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var totalOrders = await _context.Orders.CountAsync();
                var totalProducts = await _context.Products.CountAsync();
                var totalCategories = await _context.Categories.CountAsync();
                var totalUsers = await _context.Users.CountAsync();

                var totalRevenue = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                    .SumAsync(o => o.TotalAmount);

                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        o.TotalAmount,
                        o.Status,
                        o.OrderDate,
                        CustomerName = $"{o.User.FirstName} {o.User.LastName}",
                        CustomerEmail = o.User.Email,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        ProductName = g.First().Product.Name,
                        TotalSold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.TotalPrice)
                    })
                    .OrderByDescending(p => p.TotalSold)
                    .Take(5)
                    .ToListAsync();

                var ordersByStatus = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                var monthlyRevenue = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                    .Where(o => o.OrderDate >= DateTime.Now.AddMonths(-12))
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(m => m.Month)
                    .ToListAsync();

                var dashboard = new
                {
                    TotalOrders = totalOrders,
                    TotalProducts = totalProducts,
                    TotalCategories = totalCategories,
                    TotalUsers = totalUsers,
                    TotalRevenue = totalRevenue,
                    RecentOrders = recentOrders,
                    TopProducts = topProducts,
                    OrdersByStatus = ordersByStatus,
                    MonthlyRevenue = monthlyRevenue
                };

                return Ok(new
                {
                    Message = "Dashboard data retrieved successfully",
                    Data = dashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => 
                        u.Email.Contains(search) ||
                        u.FirstName.Contains(search) ||
                        u.LastName.Contains(search));
                }

                if (!string.IsNullOrEmpty(role))
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    var userIdsInRole = usersInRole.Select(u => u.Id).ToList();
                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = new List<object>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var orderCount = await _context.Orders.CountAsync(o => o.UserId == user.Id);

                    userDtos.Add(new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.PhoneNumber,
                        user.Address,
                        user.City,
                        user.Country,
                        user.CreatedAt,
                        user.EmailConfirmed,
                        Roles = roles,
                        OrderCount = orderCount
                    });
                }

                return Ok(new
                {
                    Message = "Users retrieved successfully",
                    Data = new
                    {
                        Users = userDtos,
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
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { Message = "User not found" });

                var roles = await _userManager.GetRolesAsync(user);

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == id)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        o.TotalAmount,
                        o.Status,
                        o.OrderDate,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                var userDetails = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    user.Address,
                    user.City,
                    user.PostalCode,
                    user.Country,
                    user.CreatedAt,
                    user.EmailConfirmed,
                    Roles = roles,
                    Orders = orders,
                    TotalOrders = await _context.Orders.CountAsync(o => o.UserId == id),
                    TotalSpent = await _context.Orders
                        .Where(o => o.UserId == id && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                        .SumAsync(o => o.TotalAmount)
                };

                return Ok(new
                {
                    Message = "User details retrieved successfully",
                    Data = userDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user details for ID {UserId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("users/{id}/lock")]
        public async Task<IActionResult> LockUser(string id, [FromQuery] int days = 30)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { Message = "User not found" });

                var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(days));
                if (!result.Succeeded)
                    return BadRequest(new { Message = "Failed to lock user", Errors = GetIdentityErrors(result) });

                return Ok(new { Message = $"User locked for {days} days" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user with ID {UserId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("users/{id}/unlock")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { Message = "User not found" });

                var result = await _userManager.SetLockoutEndDateAsync(user, null);
                if (!result.Succeeded)
                    return BadRequest(new { Message = "Failed to unlock user", Errors = GetIdentityErrors(result) });

                return Ok(new { Message = "User unlocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user with ID {UserId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            try
            {
                var lastMonth = DateTime.Now.AddMonths(-1);
                var lastYear = DateTime.Now.AddYears(-1);

                // Sales analytics
                var salesThisMonth = await _context.Orders
                    .Where(o => o.OrderDate >= lastMonth && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                    .SumAsync(o => o.TotalAmount);

                var salesLastMonth = await _context.Orders
                    .Where(o => o.OrderDate >= lastMonth.AddMonths(-1) && o.OrderDate < lastMonth && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                    .SumAsync(o => o.TotalAmount);

                var salesThisYear = await _context.Orders
                    .Where(o => o.OrderDate >= lastYear && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                    .SumAsync(o => o.TotalAmount);

                // Order analytics
                var ordersThisMonth = await _context.Orders
                    .CountAsync(o => o.OrderDate >= lastMonth);

                var ordersLastMonth = await _context.Orders
                    .CountAsync(o => o.OrderDate >= lastMonth.AddMonths(-1) && o.OrderDate < lastMonth);

                // Customer analytics
                var newCustomersThisMonth = await _context.Users
                    .CountAsync(u => u.CreatedAt >= lastMonth);

                var newCustomersLastMonth = await _context.Users
                    .CountAsync(u => u.CreatedAt >= lastMonth.AddMonths(-1) && u.CreatedAt < lastMonth);

                // Product analytics
                var topSellingProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        ProductName = g.First().Product.Name,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.TotalPrice)
                    })
                    .OrderByDescending(p => p.QuantitySold)
                    .Take(10)
                    .ToListAsync();

                var topCustomers = await _context.Orders
                    .Include(o => o.User)
                    .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                    .GroupBy(o => o.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        CustomerName = $"{g.First().User.FirstName} {g.First().User.LastName}",
                        CustomerEmail = g.First().User.Email,
                        TotalOrders = g.Count(),
                        TotalSpent = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(10)
                    .ToListAsync();

                // Category performance
                var categoryPerformance = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                    .GroupBy(oi => oi.Product.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().Product.Category.Name,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.TotalPrice)
                    })
                    .OrderByDescending(c => c.Revenue)
                    .ToListAsync();

                var analytics = new
                {
                    Sales = new
                    {
                        ThisMonth = salesThisMonth,
                        LastMonth = salesLastMonth,
                        ThisYear = salesThisYear,
                        MonthlyGrowth = salesLastMonth > 0 ? (salesThisMonth - salesLastMonth) / salesLastMonth * 100 : 0
                    },
                    Orders = new
                    {
                        ThisMonth = ordersThisMonth,
                        LastMonth = ordersLastMonth,
                        MonthlyGrowth = ordersLastMonth > 0 ? (ordersThisMonth - ordersLastMonth) / (double)ordersLastMonth * 100 : 0
                    },
                    Customers = new
                    {
                        NewThisMonth = newCustomersThisMonth,
                        NewLastMonth = newCustomersLastMonth,
                        MonthlyGrowth = newCustomersLastMonth > 0 ? (newCustomersThisMonth - newCustomersLastMonth) / (double)newCustomersLastMonth * 100 : 0
                    },
                    TopSellingProducts = topSellingProducts,
                    TopCustomers = topCustomers,
                    CategoryPerformance = categoryPerformance
                };

                return Ok(new
                {
                    Message = "Analytics data retrieved successfully",
                    Data = analytics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics data");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = new
                {
                    SiteName = "ECommerceShop",
                    SiteDescription = "Ваш надежный интернет-магазин",
                    ContactEmail = "info@ecommerceshop.ru",
                    ContactPhone = "+7 (999) 123-45-67",
                    Currency = "RUB",
                    TaxRate = 0.2m,
                    ShippingCost = 10.00m,
                    FreeShippingThreshold = 1000.00m,
                    DefaultPageSize = 20,
                    MaxFileSize = 5 * 1024 * 1024, // 5MB
                    AllowedImageFormats = new[] { "jpg", "jpeg", "png", "gif", "webp" },
                    MaintenanceMode = false,
                    AllowRegistration = true,
                    RequireEmailConfirmation = false,
                    AutoApproveReviews = true
                };

                return Ok(new
                {
                    Message = "Settings retrieved successfully",
                    Data = settings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        private List<string> GetIdentityErrors(IdentityResult result)
        {
            return result.Errors
                .Select(e => e.Description)
                .ToList();
        }
    }
}