using ECommerceShop.Data;
using ECommerceShop.Models;
using ECommerceShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceShop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            var shoppingCart = await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(sc => sc.UserId == user.Id);

            if (shoppingCart == null || !shoppingCart.CartItems.Any())
            {
                TempData["Error"] = "Ваша корзина пуста";
                return RedirectToAction("ShoppingCart", "Home");
            }

            // Check stock availability
            foreach (var item in shoppingCart.CartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Недостаточно товара '{item.Product.Name}' на складе";
                    return RedirectToAction("ShoppingCart", "Home");
                }
            }

            var viewModel = new CheckoutViewModel
            {
                ShoppingCart = shoppingCart,
                User = user,
                ShippingAddress = user.Address,
                ShippingCity = user.City,
                ShippingPostalCode = user.PostalCode,
                ShippingCountry = user.Country,
                PhoneNumber = user.PhoneNumber
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var shoppingCart = await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(sc => sc.UserId == user.Id);

            if (shoppingCart == null || !shoppingCart.CartItems.Any())
            {
                TempData["Error"] = "Ваша корзина пуста";
                return RedirectToAction("ShoppingCart", "Home");
            }

            if (!ModelState.IsValid)
            {
                model.ShoppingCart = shoppingCart;
                model.User = user;
                return View(model);
            }

            // Create order
            var order = new Order
            {
                UserId = user.Id,
                OrderNumber = GenerateOrderNumber(),
                TotalAmount = shoppingCart.Total,
                TaxAmount = shoppingCart.Total * 0.2m, // 20% tax
                ShippingAmount = 10.00m, // Fixed shipping cost
                ShippingAddress = model.ShippingAddress,
                ShippingCity = model.ShippingCity,
                ShippingPostalCode = model.ShippingPostalCode,
                ShippingCountry = model.ShippingCountry,
                Status = OrderStatus.Pending,
                Notes = model.Notes
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

            TempData["Success"] = $"Заказ #{order.OrderNumber} успешно оформлен!";
            return RedirectToAction(nameof(OrderDetails), new { id = order.Id });
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Нельзя отменить заказ в текущем статусе";
                return RedirectToAction(nameof(OrderDetails), new { id });
            }

            // Restore stock
            foreach (var item in order.OrderItems)
            {
                item.Product.StockQuantity += item.Quantity;
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Заказ отменен";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }
    }
}