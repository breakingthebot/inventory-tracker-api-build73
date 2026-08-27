// tests/InventoryTracker.Tests/Controllers/BomControllerTests.cs
// Unit tests for BomController REST action endpoints and assembly workflows.
// Connects to: src/InventoryTracker.Api/Controllers/BomController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class BomControllerTests
{
    [Fact]
    public async Task GetProductBom_ExistingId_ReturnsOkWithBomDetails()
    {
        // Arrange
        var mockService = new Mock<IBomService>();
        var detailsDto = new ProductBomDetailsDto
        {
            ParentProductId = 1,
            ParentSku = "KIT-BUNDLE-01",
            RolledUpMaterialCost = 150m,
            MaxAssemblableKits = 10
        };

        mockService.Setup(s => s.GetProductBomAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailsDto);

        var controller = new BomController(mockService.Object);

        // Act
        var result = await controller.GetProductBom(1, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<ProductBomDetailsDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(150m, envelope.Data?.RolledUpMaterialCost);
        Assert.Equal(10, envelope.Data?.MaxAssemblableKits);
    }

    [Fact]
    public async Task AssembleKit_ValidPayload_ReturnsOkWithAssemblyResult()
    {
        // Arrange
        var mockService = new Mock<IBomService>();
        var requestDto = new AssembleKitRequestDto
        {
            KitProductId = 1,
            WarehouseId = 1,
            Quantity = 5,
            LaborCost = 50m
        };
        var resultDto = new AssembleKitResultDto
        {
            AssemblyNumber = "ASM-20260827-TEST",
            KitProductId = 1,
            KitSku = "KIT-BUNDLE-01",
            QuantityAssembled = 5,
            RolledUpUnitCost = 160m
        };

        mockService.Setup(s => s.AssembleKitAsync(requestDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var controller = new BomController(mockService.Object);

        // Act
        var result = await controller.AssembleKit(requestDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<AssembleKitResultDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(5, envelope.Data?.QuantityAssembled);
        Assert.Equal(160m, envelope.Data?.RolledUpUnitCost);
    }
}
