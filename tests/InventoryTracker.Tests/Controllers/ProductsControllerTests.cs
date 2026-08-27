// tests/InventoryTracker.Tests/Controllers/ProductsControllerTests.cs
// Unit tests for ProductsController REST action methods and HTTP status responses.
// Connects to: src/InventoryTracker.Api/Controllers/ProductsController.cs
// Created: 2026-08-26

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetProductById_ExistingId_ReturnsOkWithProduct()
    {
        // Arrange
        var mockService = new Mock<IProductService>();
        var productDto = new ProductDto
        {
            Id = 1,
            Sku = "ELEC-01",
            Name = "Wireless Mouse",
            UnitPrice = 29.99m,
            QuantityInStock = 50
        };
        mockService.Setup(s => s.GetProductByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(productDto);

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.GetProductById(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<ProductDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal("ELEC-01", envelope.Data?.Sku);
    }

    [Fact]
    public async Task GetProductById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var mockService = new Mock<IProductService>();
        mockService.Setup(s => s.GetProductByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.GetProductById(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(envelope.Success);
    }

    [Fact]
    public async Task CreateProduct_ValidPayload_ReturnsCreatedAtAction()
    {
        // Arrange
        var mockService = new Mock<IProductService>();
        var createDto = new CreateProductDto
        {
            Sku = "NEW-01",
            Name = "New Item",
            CategoryId = 1,
            UnitPrice = 10m
        };
        var createdDto = new ProductDto
        {
            Id = 10,
            Sku = "NEW-01",
            Name = "New Item",
            UnitPrice = 10m
        };

        mockService.Setup(s => s.CreateProductAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.CreateProduct(createDto, CancellationToken.None);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        var envelope = Assert.IsType<ApiResponse<ProductDto>>(createdAtResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(10, envelope.Data?.Id);
    }
}
