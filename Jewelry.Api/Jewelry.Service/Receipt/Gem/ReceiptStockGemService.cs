using jewelry.Model.Exceptions;
using jewelry.Model.Receipt.Gem.Create;
using jewelry.Model.Receipt.Gem.Scan;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt.Gem
{
    public interface IReceiptStockGemService
    {
        Task<string> CreateGem(CreateRequest request);
        Task<IQueryable<ScanResponse>> Scan(ScanRequest request);
    }
    public class ReceiptStockGemService : IReceiptStockGemService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public ReceiptStockGemService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }

        public async Task<string> CreateGem(CreateRequest request)
        {
            var gem = (from item in _jewelryContext.TbtStockGem
                       where item.Code == request.Code.ToUpper().Trim()
                       select item);

            if (gem.Any())
            {
                throw new HandleException("รหัสเพชรเเละพลอยซ้ำ");
            }

            var newGem = new TbtStockGem()
            {
                Code = request.Code.ToUpper().Trim(),
                GroupName = request.GroupName.Trim(),

                Size = request.Size.Trim(),
                Shape = request.Shape,
                Grade = request.Grade,
                GradeCode = request.GradeCode,

                CreateDate = DateTime.UtcNow,
                CreateBy = _admin,

                Remark1 = request.Remark.Trim(),

                Price = 0,
                PriceQty = 0,
                Quantity = 0,
            };
            _jewelryContext.TbtStockGem.Add(newGem);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }
        public async Task<IQueryable<ScanResponse>> Scan(ScanRequest request)
        {
            var query = from item in _jewelryContext.TbtStockGem
                        select new ScanResponse()
                        {
                            Id = item.Id,
                            Name = $"{item.Code}-{item.Shape}-{item.Size}-{item.Grade}",
                            Code = item.Code,
                            GroupName = item.GroupName,

                            Size = item.Size,
                            Shape = item.Shape,
                            Grade = item.Grade,

                            Quantity = item.Quantity,
                            QuantityOnProcess = item.QuantityOnProcess,
                            Price = item.Price,
                            PriceQty = item.PriceQty,
                            Unit = item.Unit,
                            UnitCode = item.UnitCode,

                            Remark1 = item.Remark1,
                            Remark2 = item.Remark2,

                            Wg = item.Wg,
                            Daterec = item.Daterec,
                            Original = item.Original,
                        };

            if (request.Scans.Any())
            {
                var codes = request.Scans.Select(x => x.Code.ToUpperInvariant()).ToArray();

                if (request.ScanType == "S")
                {
                    //var code = codes[0];
                    query = (from item in query
                             where item.Code == codes[0]
                             select item);
                }
                else
                {
                    query = (from item in query
                             where codes.Contains(item.Code)
                             select item);
                }
            }

            var testtt = query.ToList();
            return query;
        }
    }
}
