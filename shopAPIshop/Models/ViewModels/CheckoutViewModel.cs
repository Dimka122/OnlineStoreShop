using ECommerceShop.Models;
using System.ComponentModel.DataAnnotations;

namespace ECommerceShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public ShoppingCart ShoppingCart { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        [Required(ErrorMessage = "Адрес доставки обязателен")]
        [StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Город обязателен")]
        [StringLength(100)]
        public string ShippingCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Почтовый индекс обязателен")]
        [StringLength(20)]
        public string ShippingPostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Страна обязательна")]
        [StringLength(100)]
        public string ShippingCountry { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Неверный формат телефона")]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}