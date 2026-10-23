using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock.Rfid;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Stock.Rfid;

public class StockRfidServiceTests
{
    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new JewelryContext(options);

        context.TbtStockPiece.AddRange(
            new TbtStockPiece { StockNumber = "DK001", ProductCode = "P1", SkuCode = "SKU1", LocationCode = "L1", Status = "IN_STOCK", CreateBy = "t" },
            new TbtStockPiece { StockNumber = "DK002", ProductCode = "P1", SkuCode = "SKU1", LocationCode = "L1", Status = "IN_STOCK", CreateBy = "t" });
        context.SaveChanges();

        return context;
    }

    private static StockRfidService CreateService(JewelryContext context)
        => new(context, new StubHttpContextAccessor());

    private static jewelry.Model.Stock.Rfid.Bind.Request BindRequest(string epc, string stockNumber, string? tid = null)
        => new() { Epc = epc, Tid = tid, StockNumber = stockNumber, ProductCode = "P1" };

    [Fact]
    public async Task Bind_Success_ReturnsBinding()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.Bind(BindRequest("E200001", "DK001", "TID1"));

        Assert.True(result.Id > 0);
        Assert.Equal("E200001", result.Epc);
        Assert.Equal("TID1", result.Tid);
        Assert.Equal("DK001", result.StockNumber);
        Assert.Equal("P1", result.ProductCode);

        var saved = await context.TbtStockPieceRfid.SingleAsync();
        Assert.True(saved.IsActive);
        Assert.Equal(DateTimeKind.Utc, saved.CreateDate.Kind);
    }

    [Fact]
    public async Task Bind_PieceNotFound_Throws()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<HandleException>(() => service.Bind(BindRequest("E1", "NOPE")));
    }

    [Fact]
    public async Task Bind_EmptyEpc_Throws()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<HandleException>(() => service.Bind(BindRequest("  ", "DK001")));
    }

    [Fact]
    public async Task Bind_DuplicateEpc_ThrowsWithExistingStockNumber()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(BindRequest("E200001", "DK001"));

        var ex = await Assert.ThrowsAsync<HandleException>(() => service.Bind(BindRequest("e200001", "DK002")));

        Assert.Contains("DK001", ex.Message);
    }

    [Fact]
    public async Task Bind_PieceAlreadyTagged_ThrowsWithExistingEpc()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(BindRequest("E200001", "DK001"));

        var ex = await Assert.ThrowsAsync<HandleException>(() => service.Bind(BindRequest("E200002", "DK001")));

        Assert.Contains("E200001", ex.Message);
    }

    [Fact]
    public async Task Unbind_ThenRebind_Works()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(BindRequest("E200001", "DK001"));

        var unbind = await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { Epc = "E200001" });
        Assert.True(unbind.Unbound);

        var again = await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { Epc = "E200001" });
        Assert.False(again.Unbound);

        var rebindSamePiece = await service.Bind(BindRequest("E200002", "DK001"));
        Assert.Equal("E200002", rebindSamePiece.Epc);

        var rebindOldEpc = await service.Bind(BindRequest("E200001", "DK002"));
        Assert.Equal("DK002", rebindOldEpc.StockNumber);

        Assert.Equal(1, await context.TbtStockPieceRfid.CountAsync(r => !r.IsActive && r.UpdateBy != null && r.UpdateDate != null));
    }

    [Fact]
    public async Task Unbind_ByStock_Works()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(BindRequest("E200001", "DK001"));

        var result = await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { StockNumber = "DK001", ProductCode = "P1" });

        Assert.True(result.Unbound);
        var byStock = await service.GetByStock(new jewelry.Model.Stock.Rfid.GetByStock.Request { StockNumber = "DK001", ProductCode = "P1" });
        Assert.False(byStock.Found);
    }

    [Fact]
    public async Task Bind_NormalizesEpcAndTid()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.Bind(BindRequest("  e2 00 0a b1 \t", "DK001", " 11 aa "));

        Assert.Equal("E2000AB1", result.Epc);
        Assert.Equal("11AA", result.Tid);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData(" ab cd ", "ABCD")]
    public void NormalizeTag_Works(string? input, string? expected)
    {
        Assert.Equal(expected, StockRfidService.NormalizeTag(input));
    }

    [Fact]
    public async Task GetByStock_AfterBind_ReturnsTag()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(BindRequest("E200001", "DK001", "T1"));

        var result = await service.GetByStock(new jewelry.Model.Stock.Rfid.GetByStock.Request { StockNumber = "DK001", ProductCode = "P1" });

        Assert.True(result.Found);
        Assert.Equal("E200001", result.Epc);
        Assert.Equal("T1", result.Tid);
        Assert.NotNull(result.BoundDate);
    }
}

internal class StubHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}
