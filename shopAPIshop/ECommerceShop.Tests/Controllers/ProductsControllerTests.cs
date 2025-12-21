using ECommerceShop.Controllers;
using ECommerceShop.Data;
using ECommerceShop.Models;
using ECommerceShop.Models.ViewModels;
using ECommerceShop.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore.InMemory;

namespace ECommerceShop.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ProductsController _controller;
        private readonly Mock<ILogger<ProductsController>> _mockLogger;

        public ProductsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            
            // Seed test data
            SeedTestData();

            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _mockLogger = new Mock<ILogger<ProductsController>>();
            _controller = new ProductsController(_context, _mockUserManager.Object, _mockLogger.Object);
        }

        private void SeedTestData()
        {
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            // Add test category
            var category = new Category
            {
                Name = "Electronics",
                Description = "Electronic devices",
                IsActive = true
            };
            _context.Categories.Add(category);
            _context.SaveChanges();

            // Add test products
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Smartphone",
                    Description = "A modern smartphone",
                    Price = 999.99m,
                    StockQuantity = 50,
                    CategoryId = category.Id,
                    IsActive = true,
                    IsFeatured = true
                },
                new Product
                {
                    Name = "Laptop",
                    Description = "A powerful laptop",
                    Price = 1499.99m,
                    StockQuantity = 30,
                    CategoryId = category.Id,
                    IsActive = true,
                    IsFeatured = false
                }
            };

            _context.Products.AddRange(products);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetProducts_ReturnsOkResult_WithProducts()
        {
            // Act
            var result = await _controller.GetProducts();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var response = okResult.Value;

            Assert.IsNotNull(response);
        }

        [Fact]
        public async Task GetProduct_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var productId = _context.Products.First().Id;

            // Act
            var result = await _controller.GetProduct(productId);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var response = okResult.Value;

            Assert.IsNotNull(response);
        }

        [Fact]
        public async Task GetProduct_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = 999;

            // Act
            var result = await _controller.GetProduct(invalidId);

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            Assert.IsNotNull(notFoundResult);
            var response = notFoundResult.Value;

            Assert.IsNotNull(response);
        }

        [Fact]
        public async Task GetRelatedProducts_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var productId = _context.Products.First().Id;

            // Act
            var result = await _controller.GetRelatedProducts(productId);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var response = okResult.Value;

            Assert.IsNotNull(response);
        }

        [Fact]
        public async Task GetProducts_WithCategoryFilter_ReturnsFilteredProducts()
        {
            // Arrange
            var categoryId = _context.Categories.First().Id;

            // Act
            var result = await _controller.GetProducts(categoryId: categoryId);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var response = okResult.Value;

            Assert.IsNotNull(response);
        }

        [Fact]
        public async Task GetProducts_WithSearchFilter_ReturnsFilteredProducts()
        {
            // Arrange
            var searchTerm = "Smart";

            // Act
            var result = await _controller.GetProducts(search: searchTerm);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var response = okResult.Value;

            Assert.IsNotNull(response);
        }

        [Fact]
        public async Task GetProducts_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            var page = 1;
            var pageSize = 1;

            // Act
            var result = await _controller.GetProducts(page: page, pageSize: pageSize);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var response = okResult.Value;

            Assert.IsNotNull(response);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}