using jewelry.Model.Exceptions;
using jewelry.Model.Receipt.Gem.Create;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.Extensions.Hosting;
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
    }
}
