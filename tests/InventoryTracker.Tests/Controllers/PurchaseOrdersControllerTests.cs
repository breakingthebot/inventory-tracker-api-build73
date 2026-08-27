// tests/InventoryTracker.Tests/Controllers/PurchaseOrdersControllerTests.cs
// Unit tests for PurchaseOrdersController action methods and API status responses.
// Connects to: src/InventoryTracker.Api/Controllers/PurchaseOrdersController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class PurchaseOrdersControllerTests
{
    [Fact]
    public async Task GetReorderSuggestions_ReturnsOkWithSuggestions()
    {
        // Arrange
        var mockService = new Mock<IPurchaseOrderService>();
        var suggestions = new List<AutoReorderSuggestionDto>
        {
            new() { ProductId = 1, ProductSku = "SKU-1", CurrentTotalStock = 2, ReorderThreshold = 10, RecommendedOrderQuantity = 50 }
        };

        mockService.Setup(s => s.GetAutoReorderSuggestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        var controller = new PurchaseOrdersController(mockService.Object);

        // Act
        var result = await controller.GetReorderSuggestions(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<IReadOnlyList<AutoReorderSuggestionDto>>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Single(envelope.Data!);
    }

    [Fact]
    public async Task SubmitPurchaseOrder_ValidId_ReturnsOk()
    {
        // Arrange
        var mockService = new Mock<IPurchaseOrderService>();
        var poDto = new PurchaseOrderDto
        {
            Id = 5,
            OrderNumber = "PO-2026-0005",
            Status = PurchaseOrderStatus.Submitted
        };

        mockService.Setup(s => s.SubmitPurchaseOrderAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poDto);

        var controller = new PurchaseOrdersController(mockService.Object);

        // Act
        var result = await controller.SubmitPurchaseOrder(5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<PurchaseOrderDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(PurchaseOrderStatus.Submitted, envelope.Data?.Status);
    }
}
