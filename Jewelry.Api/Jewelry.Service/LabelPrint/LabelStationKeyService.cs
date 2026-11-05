using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Jewelry.Service.LabelPrint
{
    public interface ILabelStationKeyService
    {
        Task<string?> GetActiveKeyCached();
        Task<jewelry.Model.LabelStationKey.Get.Response> Get(jewelry.Model.LabelStationKey.Get.Request request);
        Task<jewelry.Model.LabelStationKey.Generate.Response> Generate(jewelry.Model.LabelStationKey.Generate.Request request);
    }

    public class LabelStationKeyService : BaseService, ILabelStationKeyService
    {
        private const string CacheKey = "label-station-active-key";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private sealed class KeyHolder
        {
            public string? Value { get; set; }
        }

        public LabelStationKeyService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        public async Task<string?> GetActiveKeyCached()
        {
            if (_cache.TryGetValue(CacheKey, out KeyHolder? cached) && cached != null)
            {
                return cached.Value;
            }

            var key = await _jewelryContext.TbtLabelStationKey
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.DeviceKey)
                .FirstOrDefaultAsync();

            _cache.Set(CacheKey, new KeyHolder { Value = key }, CacheTtl);
            return key;
        }

        public async Task<jewelry.Model.LabelStationKey.Get.Response> Get(jewelry.Model.LabelStationKey.Get.Request request)
        {
            var active = await _jewelryContext.TbtLabelStationKey
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive);

            return new jewelry.Model.LabelStationKey.Get.Response
            {
                DeviceKey = active?.DeviceKey,
                CreateDate = active?.CreateDate,
                CreateBy = active?.CreateBy,
            };
        }

        public async Task<jewelry.Model.LabelStationKey.Generate.Response> Generate(jewelry.Model.LabelStationKey.Generate.Request request)
        {
            var now = DateTime.UtcNow;

            var actives = await _jewelryContext.TbtLabelStationKey
                .Where(x => x.IsActive)
                .ToListAsync();

            foreach (var old in actives)
            {
                old.IsActive = false;
                old.RevokedDate = now;
                old.RevokedBy = CurrentUsername;
            }
            if (actives.Count > 0)
            {
                _jewelryContext.TbtLabelStationKey.UpdateRange(actives);
            }

            var created = new TbtLabelStationKey
            {
                DeviceKey = GenerateKey(),
                IsActive = true,
                CreateDate = now,
                CreateBy = CurrentUsername,
            };
            _jewelryContext.TbtLabelStationKey.Add(created);

            await _jewelryContext.SaveChangesAsync();
            _cache.Remove(CacheKey);

            return new jewelry.Model.LabelStationKey.Generate.Response
            {
                DeviceKey = created.DeviceKey,
                CreateDate = created.CreateDate,
                CreateBy = created.CreateBy,
            };
        }

        private static string GenerateKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
