using ECommerceShop.Models;
using System.ComponentModel.DataAnnotations;

namespace ECommerceShop.DTOs
{
    public class ShoppingCartDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<CartItemDTO> CartItems { get; set; } = new List<CartItemDTO>();
        public decimal Total => CartItems.Sum(item => item.Subtotal);
        public int TotalItems => CartItems.Sum(item => item.Quantity);
    }

    public class CartItemDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CurrentPrice => SalePrice > 0 ? SalePrice : UnitPrice;
        public decimal Subtotal => CurrentPrice * Quantity;
        public bool InStock { get; set; }
        public int AvailableStock { get; set; }
    }

    public class AddToCartDTO
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemDTO
    {
        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class CartOperationResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ShoppingCartDTO? ShoppingCart { get; set; }
    }
}