using ECommerceShop.Models;

namespace ECommerceShop.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<Product> NewProducts { get; set; } = new List<Product>();
        public List<Product> SaleProducts { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
    }

    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
        public List<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}