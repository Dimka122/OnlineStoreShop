namespace ECommerceShop.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        public int ShoppingCartId { get; set; }
        
        public int ProductId { get; set; }
        
        public int Quantity { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ShoppingCart ShoppingCart { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        
        // Computed properties
        public decimal UnitPrice => Product.SalePrice ?? Product.Price;
        public decimal Subtotal => UnitPrice * Quantity;
    }
}