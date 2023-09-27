using jewelry.Model.Master;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Master
{
    public interface IMasterService
    {
        IQueryable<MasterModel> MasterGold();
        IQueryable<MasterModel> MasterGoldSize();
        IQueryable<MasterModel> MasterGem();
        IQueryable<MasterModel> MasterGemShape();
        IQueryable<MasterModel> MasterProductType();
        IQueryable<MasterModel> MasterCustomerType();
    }

    public class MasterService : IMasterService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public MasterService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }
        public IQueryable<MasterModel> MasterGold()
        {
            var response = (from item in _jewelryContext.TbmGold
                            where item.IsActive == true
                            select new MasterModel()
                            { 
                                Id = item.Id,
                                NameEn = item.NameEn,
                                NameTh = item.NameTh,
                                Code = item.Code,
                                Description = $"{item.Code}: {item.NameTh}"
                            });

            return response.OrderBy(x => x.Code);
        }
        public IQueryable<MasterModel> MasterGoldSize()
        {
            var response = (from item in _jewelryContext.TbmGoldSize
                            where item.IsActive == true
                            select new MasterModel()
                            {
                                Id = item.Id,
                                NameEn = item.NameEn,
                                NameTh = item.NameTh,
                                Code = item.Code,
                                Description = item.NameEn
                            });

            return response.OrderBy(x => x.NameEn);
        }
        public IQueryable<MasterModel> MasterGem()
        {
            var response = (from item in _jewelryContext.TbmGem
                            where item.IsActive == true
                            select new MasterModel()
                            {
                                Id = item.Id,
                                NameEn = item.NameEn,
                                NameTh = item.NameTh,
                                Code = item.Code,
                                Description = $"{item.Code}: {item.NameTh}"
                            });

            return response.OrderBy(x => x.Code);
        }
        public IQueryable<MasterModel> MasterGemShape()
        {
            var response = (from item in _jewelryContext.TbmGemShape
                            where item.IsActive == true
                            select new MasterModel()
                            {
                                Id = item.Id,
                                NameEn = item.NameEn,
                                NameTh = item.NameTh,
                                Code = item.Code,
                                Description = $"{item.Code}: {item.NameTh}"
                            });

            return response.OrderBy(x => x.Code);
        }
        public IQueryable<MasterModel> MasterProductType()
        {
            var response = (from item in _jewelryContext.TbmProductType
                            where item.IsActive == true
                            select new MasterModel()
                            {
                                Id = item.Id,
                                NameEn = item.NameEn,
                                NameTh = item.NameTh,
                                Code = item.Code,
                                Description = $"{item.Code}: {item.NameTh}"
                            });

            return response.OrderBy(x => x.Code);
        }
        public IQueryable<MasterModel> MasterCustomerType()
        {
            var response = (from item in _jewelryContext.TbmCustomerType
                            where item.IsActive == true
                            select new MasterModel()
                            {
                                Id = item.Id,
                                NameEn = item.NameEn,
                                NameTh = item.NameTh,
                                Code = item.Code,
                                Description = $"{item.Code}: {item.NameTh}"
                            });

            return response.OrderBy(x => x.Code);
        }
    }
}
