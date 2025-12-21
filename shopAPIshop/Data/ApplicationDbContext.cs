using ECommerceShop.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerceShop.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Category
            builder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Product
            builder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Price).HasPrecision(10, 2);
                entity.Property(e => e.SalePrice).HasPrecision(10, 2);
                
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ShoppingCart
            builder.Entity<ShoppingCart>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithOne(u => u.ShoppingCart)
                      .HasForeignKey<ShoppingCart>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CartItem
            builder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.ShoppingCart)
                      .WithMany(sc => sc.CartItems)
                      .HasForeignKey(e => e.ShoppingCartId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.CartItems)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Order
            builder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
                entity.Property(e => e.TaxAmount).HasPrecision(10, 2);
                entity.Property(e => e.ShippingAmount).HasPrecision(10, 2);
                entity.Property(e => e.ShippingAddress).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ShippingCity).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ShippingPostalCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ShippingCountry).IsRequired().HasMaxLength(100);
                
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OrderItem
            builder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(10, 2);
                
                entity.HasOne(e => e.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ProductReview
            builder.Entity<ProductReview>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Comment).IsRequired().HasMaxLength(1000);
                
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.Reviews)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial data
            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
        {
            // Seed Categories
            var electronicsCategory = new Category 
            { 
                Id = 1, 
                Name = "Электроника", 
                Description = "Смартфоны, ноутбуки и другие электронные устройства",
                IsActive = true 
            };
            
            var clothingCategory = new Category 
            { 
                Id = 2, 
                Name = "Одежда", 
                Description = "Мужская, женская и детская одежда",
                IsActive = true 
            };
            
            var booksCategory = new Category 
            { 
                Id = 3, 
                Name = "Книги", 
                Description = "Художественная и техническая литература",
                IsActive = true 
            };

            builder.Entity<Category>().HasData(electronicsCategory, clothingCategory, booksCategory);

            // Seed Products
            builder.Entity<Product>().HasData(
                new Product 
                { 
                    Id = 1, 
                    Name = "Смартфон Pro Max", 
                    Description = "Флагманский смартфон с продвинутой камерой и процессором",
                    Price = 999.99m,
                    StockQuantity = 50,
                    CategoryId = 1,
                    IsActive = true,
                    IsFeatured = true
                },
                new Product 
                { 
                    Id = 2, 
                    Name = "Ноутбук Ultra", 
                    Description = "Тонкий и мощный ноутбук для работы и развлечений",
                    Price = 1499.99m,
                    StockQuantity = 30,
                    CategoryId = 1,
                    IsActive = true,
                    IsFeatured = true
                },
                new Product 
                { 
                    Id = 3, 
                    Name = "Футболка Premium", 
                    Description = "Качественная хлопковая футболка",
                    Price = 29.99m,
                    SalePrice = 19.99m,
                    StockQuantity = 100,
                    CategoryId = 2,
                    IsActive = true
                },
                new Product 
                { 
                    Id = 4, 
                    Name = "Джинсы Classic", 
                    Description = "Классические джинсы прямого кроя",
                    Price = 79.99m,
                    StockQuantity = 75,
                    CategoryId = 2,
                    IsActive = true
                },
                new Product 
                { 
                    Id = 5, 
                    Name = "Программирование на C#", 
                    Description = "Полное руководство по программированию на C#",
                    Price = 49.99m,
                    StockQuantity = 200,
                    CategoryId = 3,
                    IsActive = true
                }
            );
        }
    }
}