using jewelry.Model.Setting.PrintLayout;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Setting.PrintLayout;

public class PrintLayoutService : BaseService, IPrintLayoutService
{
    private static readonly HashSet<string> AllowedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "bill", "vat"
    };

    private readonly JewelryContext _jewelryContext;

    public PrintLayoutService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
    }

    public async Task<PrintLayoutDto?> GetAsync(string key)
    {
        ValidateKey(key);

        var entity = await _jewelryContext.TbmPrintLayout
            .FirstOrDefaultAsync(x => x.LayoutKey == key);

        if (entity == null)
        {
            return null;
        }

        return new PrintLayoutDto { LayoutJson = entity.LayoutJson };
    }

    public async Task SaveAsync(string key, string layoutJson)
    {
        ValidateKey(key);

        var entity = await _jewelryContext.TbmPrintLayout
            .FirstOrDefaultAsync(x => x.LayoutKey == key);

        if (entity != null)
        {
            entity.LayoutJson = layoutJson;
            entity.UpdateBy = CurrentUsername;
            entity.UpdateDate = DateTime.UtcNow;

            _jewelryContext.TbmPrintLayout.Update(entity);
        }
        else
        {
            var newEntity = new TbmPrintLayout
            {
                LayoutKey = key,
                LayoutJson = layoutJson,
                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow
            };

            await _jewelryContext.TbmPrintLayout.AddAsync(newEntity);
        }

        await _jewelryContext.SaveChangesAsync();
    }

    private static void ValidateKey(string key)
    {
        if (!AllowedKeys.Contains(key))
        {
            throw new ArgumentException($"Invalid layout key '{key}'. Allowed keys: bill, vat.");
        }
    }
}
