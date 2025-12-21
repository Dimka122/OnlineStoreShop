namespace ECommerceShop.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        
        // Computed properties
        public decimal Total => CartItems.Sum(item => item.Subtotal);
        public int TotalItems => CartItems.Sum(item => item.Quantity);
    }
}