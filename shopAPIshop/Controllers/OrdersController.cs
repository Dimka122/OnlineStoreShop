using ECommerceShop.DTOs;
using ECommerceShop.Models;
using ECommerceShop.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] OrderStatus? status = null)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var query = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == userId);

                if (status.HasValue)
                {
                    query = query.Where(o => o.Status == status.Value);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderDTO
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        TotalAmount = o.TotalAmount,
                        TaxAmount = o.TaxAmount,
                        ShippingAmount = o.ShippingAmount,
                        ShippingAddress = o.ShippingAddress,
                        ShippingCity = o.ShippingCity,
                        ShippingPostalCode = o.ShippingPostalCode,
                        ShippingCountry = o.ShippingCountry,
                        Status = o.Status,
                        OrderDate = o.OrderDate,
                        ShippedDate = o.ShippedDate,
                        DeliveredDate = o.DeliveredDate,
                        TrackingNumber = o.TrackingNumber,
                        Notes = o.Notes,
                        User = new ECommerceShop.DTOs.UserDTO
                        {
                            Id = o.User.Id,
                            Email = o.User.Email,
                            FirstName = o.User.FirstName,
                            LastName = o.User.LastName
                        },
                        OrderItems = o.OrderItems.Select(oi => new OrderItemDTO
                        {
                            Id = oi.Id,
                            ProductId = oi.ProductId,
                            ProductName = oi.Product.Name,
                            ProductImageUrl = oi.Product.ImageUrl,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            TotalPrice = oi.TotalPrice
                        }).ToList()
                    })
                    .ToListAsync();

                var orderList = new OrderListDTO
                {
                    Orders = orders,
                    TotalCount = totalCount,
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                };

                return Ok(new
                {
                    Message = "Order history retrieved successfully",
                    Data = orderList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order history");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

                if (order == null)
                    return NotFound(new { Message = "Order not found" });

                var orderDto = new OrderDTO
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    TotalAmount = order.TotalAmount,
                    TaxAmount = order.TaxAmount,
                    ShippingAmount = order.ShippingAmount,
                    ShippingAddress = order.ShippingAddress,
                    ShippingCity = order.ShippingCity,
                    ShippingPostalCode = order.ShippingPostalCode,
                    ShippingCountry = order.ShippingCountry,
                    Status = order.Status,
                    OrderDate = order.OrderDate,
                    ShippedDate = order.ShippedDate,
                    DeliveredDate = order.DeliveredDate,
                    TrackingNumber = order.TrackingNumber,
                    Notes = order.Notes,
                    User = new ECommerceShop.DTOs.UserDTO
                    {
                        Id = order.User.Id,
                        Email = order.User.Email,
                        FirstName = order.User.FirstName,
                        LastName = order.User.LastName,
                        PhoneNumber = order.User.PhoneNumber
                    },
                    OrderItems = order.OrderItems.Select(oi => new OrderItemDTO
                    {
                        Id = oi.Id,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        ProductImageUrl = oi.Product.ImageUrl,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        TotalPrice = oi.TotalPrice
                    }).ToList()
                };

                return Ok(new
                {
                    Message = "Order details retrieved successfully",
                    Data = orderDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details with ID {OrderId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid order data", Errors = GetModelStateErrors() });

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var shoppingCart = await _context.ShoppingCarts
                    .Include(sc => sc.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(sc => sc.UserId == userId);

                if (shoppingCart == null || !shoppingCart.CartItems.Any())
                    return BadRequest(new { Message = "Shopping cart is empty" });

                // Check stock availability
                foreach (var cartItem in shoppingCart.CartItems)
                {
                    if (cartItem.Product.StockQuantity < cartItem.Quantity)
                        return BadRequest(new { 
                            Message = $"Insufficient stock for product '{cartItem.Product.Name}'. Only {cartItem.Product.StockQuantity} items available" 
                        });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Message = "User not found" });

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = GenerateOrderNumber(),
                    TotalAmount = shoppingCart.CartItems.Sum(ci => ci.Subtotal),
                    TaxAmount = shoppingCart.CartItems.Sum(ci => ci.Subtotal) * 0.2m, // 20% tax
                    ShippingAmount = 10.00m, // Fixed shipping cost
                    ShippingAddress = model.ShippingAddress,
                    ShippingCity = model.ShippingCity,
                    ShippingPostalCode = model.ShippingPostalCode,
                    ShippingCountry = model.ShippingCountry,
                    Status = OrderStatus.Pending,
                    Notes = model.Notes,
                    OrderDate = DateTime.UtcNow
                };

                // Create order items
                foreach (var cartItem in shoppingCart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice,
                        TotalPrice = cartItem.Subtotal
                    };
                    order.OrderItems.Add(orderItem);

                    // Update stock
                    cartItem.Product.StockQuantity -= cartItem.Quantity;
                }

                _context.Orders.Add(order);

                // Clear shopping cart
                _context.CartItems.RemoveRange(shoppingCart.CartItems);

                await _context.SaveChangesAsync();

                // Get order details for response
                var createdOrder = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstAsync(o => o.Id == order.Id);

                var orderDto = new OrderDTO
                {
                    Id = createdOrder.Id,
                    OrderNumber = createdOrder.OrderNumber,
                    TotalAmount = createdOrder.TotalAmount,
                    TaxAmount = createdOrder.TaxAmount,
                    ShippingAmount = createdOrder.ShippingAmount,
                    ShippingAddress = createdOrder.ShippingAddress,
                    ShippingCity = createdOrder.ShippingCity,
                    ShippingPostalCode = createdOrder.ShippingPostalCode,
                    ShippingCountry = createdOrder.ShippingCountry,
                    Status = createdOrder.Status,
                    OrderDate = createdOrder.OrderDate,
                    ShippedDate = createdOrder.ShippedDate,
                    DeliveredDate = createdOrder.DeliveredDate,
                    TrackingNumber = createdOrder.TrackingNumber,
                    Notes = createdOrder.Notes,
                    User = new ECommerceShop.DTOs.UserDTO
                    {
                        Id = createdOrder.User.Id,
                        Email = createdOrder.User.Email,
                        FirstName = createdOrder.User.FirstName,
                        LastName = createdOrder.User.LastName
                    },
                    OrderItems = createdOrder.OrderItems.Select(oi => new OrderItemDTO
                    {
                        Id = oi.Id,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        ProductImageUrl = oi.Product.ImageUrl,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        TotalPrice = oi.TotalPrice
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetOrderDetails), new { id = order.Id }, new
                {
                    Message = "Order created successfully",
                    Data = orderDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token" });

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

                if (order == null)
                    return NotFound(new { Message = "Order not found" });

                if (order.Status != OrderStatus.Pending)
                    return BadRequest(new { Message = "Order cannot be cancelled in current status" });

                // Restore stock
                foreach (var orderItem in order.OrderItems)
                {
                    orderItem.Product.StockQuantity += orderItem.Quantity;
                }

                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Order cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order with ID {OrderId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        // Admin endpoints
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(o => o.Status == status.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o => 
                        o.OrderNumber.Contains(search) ||
                        o.User.Email.Contains(search) ||
                        o.User.FirstName.Contains(search) ||
                        o.User.LastName.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderDTO
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        TotalAmount = o.TotalAmount,
                        TaxAmount = o.TaxAmount,
                        ShippingAmount = o.ShippingAmount,
                        ShippingAddress = o.ShippingAddress,
                        ShippingCity = o.ShippingCity,
                        ShippingPostalCode = o.ShippingPostalCode,
                        ShippingCountry = o.ShippingCountry,
                        Status = o.Status,
                        OrderDate = o.OrderDate,
                        ShippedDate = o.ShippedDate,
                        DeliveredDate = o.DeliveredDate,
                        TrackingNumber = o.TrackingNumber,
                        Notes = o.Notes,
                        User = new ECommerceShop.DTOs.UserDTO
                        {
                            Id = o.User.Id,
                            Email = o.User.Email,
                            FirstName = o.User.FirstName,
                            LastName = o.User.LastName,
                            PhoneNumber = o.User.PhoneNumber
                        },
                        OrderItems = o.OrderItems.Select(oi => new OrderItemDTO
                        {
                            Id = oi.Id,
                            ProductId = oi.ProductId,
                            ProductName = oi.Product.Name,
                            ProductImageUrl = oi.Product.ImageUrl,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            TotalPrice = oi.TotalPrice
                        }).ToList()
                    })
                    .ToListAsync();

                var orderList = new OrderListDTO
                {
                    Orders = orders,
                    TotalCount = totalCount,
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                };

                return Ok(new
                {
                    Message = "Orders retrieved successfully",
                    Data = orderList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPut("admin/{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Invalid status data", Errors = GetModelStateErrors() });

                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { Message = "Order not found" });

                order.Status = model.Status;

                if (model.Status == OrderStatus.Shipped)
                {
                    order.ShippedDate = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(model.TrackingNumber))
                    {
                        order.TrackingNumber = model.TrackingNumber;
                    }
                }
                else if (model.Status == OrderStatus.Delivered)
                {
                    order.DeliveredDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order ID {OrderId}", id);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
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