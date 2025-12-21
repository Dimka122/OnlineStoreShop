using System.ComponentModel.DataAnnotations;

namespace ECommerceShop.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsApproved { get; set; } = true;
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}