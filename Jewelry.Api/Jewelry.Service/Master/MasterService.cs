using jewelry.Model.Exceptions;
using jewelry.Model.Master;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
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


        IQueryable<MasterModel> SearchMaster(SearchMasterModel request);

        Task<string> UpdateMasterModel(UpdateEditMasterModelRequest request);
        Task<string> DeleteMasterModel(DeleteMasterModelRequest request);
        Task<string> CreateMasterModel(CreateMasterModelRequest request);
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

            var FixCode = new string[] { "RU", "SA" };
            return response.OrderByDescending(x => FixCode.Contains(x.Code)).ThenBy(x => x.Code);
            //return response.OrderByDescending(x => x.Code == "RU").ThenByDescending(x => x.Code == "SA").ThenBy(x => x.Code);
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

        // ------ search ------- //
        public IQueryable<MasterModel> SearchMaster(SearchMasterModel request)
        {

            if (request.Type == "GEM")
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


                if (!string.IsNullOrEmpty(request.Text))
                {
                    response = (from item in response
                                where item.Code.Contains(request.Text.ToUpper())
                                || item.NameTh.Contains(request.Text.ToUpper())
                                || item.NameEn.Contains(request.Text.ToUpper())
                                select item);
                }

                //var FixCode = new string[] { "RU", "SA" };
                //return response.OrderByDescending(x => FixCode.Contains(x.Code)).ThenBy(x => x.Code);
                //return response.OrderByDescending(x => x.Code == "RU").ThenByDescending(x => x.Code == "SA").ThenBy(x => x.Code);
                return response.OrderBy(x => x.Code);
            }

            else if (request.Type == "GEM-SHAPE")
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

                if (!string.IsNullOrEmpty(request.Text))
                {
                    response = (from item in response
                                where item.Code.Contains(request.Text.ToUpper())
                                || item.NameTh.Contains(request.Text.ToUpper())
                                || item.NameEn.Contains(request.Text.ToUpper())
                                select item);
                }

                return response.OrderBy(x => x.Code);
            }

            else if (request.Type.ToUpper() == "GOLD-SIZE")
            {
                var response = (from item in _jewelryContext.TbmGoldSize
                                where item.IsActive == true
                                select new MasterModel()
                                {
                                    Id = item.Id,
                                    NameEn = item.NameEn,
                                    NameTh = item.NameTh,
                                    Code = item.Code,
                                    Description = $"{item.Code}: {item.NameTh}"
                                });

                if (!string.IsNullOrEmpty(request.Text))
                {
                    response = (from item in response
                                where item.Code.Contains(request.Text.ToUpper())
                                || item.NameTh.Contains(request.Text.ToUpper())
                                || item.NameEn.Contains(request.Text.ToUpper())
                                select item);
                }

                //var FixCode = new string[] { "RU", "SA" };
                //return response.OrderByDescending(x => FixCode.Contains(x.Code)).ThenBy(x => x.Code);
                //return response.OrderByDescending(x => x.Code == "RU").ThenByDescending(x => x.Code == "SA").ThenBy(x => x.Code);
                return response.OrderBy(x => x.Code);
            }

            else if (request.Type.ToUpper() == "PRODUCT-TYPE")
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

                if (!string.IsNullOrEmpty(request.Text))
                {
                    response = (from item in response
                                where item.Code.Contains(request.Text.ToUpper())
                                || item.NameTh.Contains(request.Text.ToUpper())
                                || item.NameEn.Contains(request.Text.ToUpper())
                                select item);
                }

                return response.OrderBy(x => x.Code);
            }

            else if (request.Type.ToUpper() == "ZILL")
            {
                var response = (from item in _jewelryContext.TbmZill
                                .Include(x => x.GoldCodeNavigation)
                                .Include(x => x.GoldSizeCodeNavigation)
                                where item.IsActive == true
                                select new MasterModel()
                                {
                                    Id = item.Id,
                                    NameEn = item.NameEn ?? string.Empty,
                                    NameTh = item.NameTh ?? string.Empty,
                                    Code = item.Code,
                                    Description = item.Remark,

                                    GoldCode = item.GoldCode,
                                    GoldNameTH = item.GoldCodeNavigation.NameTh,
                                    GoldNameEN = item.GoldCodeNavigation.NameEn,

                                    GoldSizeCode = item.GoldSizeCode,
                                    GoldSizeNameTH = item.GoldSizeCodeNavigation.NameTh,
                                    GoldSizeNameEN = item.GoldSizeCodeNavigation.NameEn,
                                });

                if (!string.IsNullOrEmpty(request.Text))
                {
                    response = (from item in response
                                where item.Code.Contains(request.Text.ToUpper())
                                || item.NameTh.Contains(request.Text.ToUpper())
                                || item.NameEn.Contains(request.Text.ToUpper())
                                select item);
                }

                return response.OrderBy(x => x.Code);
            }

            throw new HandleException("Type is required.");

        }

        // ------ Update/Edit ------ //
        public async Task<string> UpdateMasterModel(UpdateEditMasterModelRequest request)
        {
            if (request.Type.ToUpper() == "GEM")
            {
                var gem = (from item in _jewelryContext.TbmGem
                           where item.Id == request.Id
                           && item.Code == request.Code.ToUpper()
                           select item).SingleOrDefault();

                if (gem == null)
                {
                    throw new HandleException($"ไม่พบข้อมูลรหัส {request.Code.ToUpper()}");
                }

                gem.NameEn = request.NameEn;
                gem.NameTh = request.NameTh;

                _jewelryContext.TbmGem.Update(gem);
                await _jewelryContext.SaveChangesAsync();
            }

            if (request.Type.ToUpper() == "GEM-SHAPE")
            {
                var gemShape = (from item in _jewelryContext.TbmGemShape
                                where item.Id == request.Id
                                && item.Code == request.Code.ToUpper()
                                select item).SingleOrDefault();

                if (gemShape == null)
                {
                    throw new HandleException($"ไม่พบข้อมูลรหัส {request.Code.ToUpper()}");
                }

                gemShape.NameEn = request.NameEn;
                gemShape.NameTh = request.NameTh;

                _jewelryContext.TbmGemShape.Update(gemShape);
                await _jewelryContext.SaveChangesAsync();
            }

            if (request.Type.ToUpper() == "GOLD-SIZE")
            {
                var goldSize = (from item in _jewelryContext.TbmGoldSize
                                where item.Id == request.Id
                                && item.Code == request.Code.ToUpper()
                                select item).SingleOrDefault();

                if (goldSize == null)
                {
                    throw new HandleException($"ไม่พบข้อมูลรหัส {request.Code.ToUpper()}");
                }

                goldSize.NameEn = request.NameEn;
                goldSize.NameTh = request.NameTh;

                _jewelryContext.TbmGoldSize.Update(goldSize);
                await _jewelryContext.SaveChangesAsync();
            }

            if (request.Type.ToUpper() == "PRODUCT-TYPE")
            {
                var productType = (from item in _jewelryContext.TbmProductType
                                   where item.Id == request.Id
                                   && item.Code == request.Code.ToUpper()
                                   select item).SingleOrDefault();

                if (productType == null)
                {
                    throw new HandleException($"ไม่พบข้อมูลรหัส {request.Code.ToUpper()}");
                }

                productType.NameEn = request.NameEn;
                productType.NameTh = request.NameTh;

                _jewelryContext.TbmProductType.Update(productType);
                await _jewelryContext.SaveChangesAsync();
            }

            return "success";
        }

        // ------ Delete ------ //
        public async Task<string> DeleteMasterModel(DeleteMasterModelRequest request)
        {
            if (request.Type.ToUpper() == "GEM")
            {
                var gem = (from item in _jewelryContext.TbmGem
                           where item.Id == request.Id
                           && item.Code == request.Code.ToUpper()
                           select item).SingleOrDefault();

                if (gem == null)
                {
                    throw new HandleException($"ไม่พอข้อมูลรหัส {request.Code.ToUpper()}");
                }

                _jewelryContext.TbmGem.Remove(gem);
                await _jewelryContext.SaveChangesAsync();
            }

            else if (request.Type.ToUpper() == "GEM-SHAPE")
            {
                var gemShape = (from item in _jewelryContext.TbmGemShape
                                where item.Id == request.Id
                                && item.Code == request.Code.ToUpper()
                                select item).SingleOrDefault();

                if (gemShape == null)
                {
                    throw new HandleException($"ไม่พอข้อมูลรหัส {request.Code.ToUpper()}");
                }

                _jewelryContext.TbmGemShape.Remove(gemShape);
                await _jewelryContext.SaveChangesAsync();
            }

            else if (request.Type.ToUpper() == "GOLD-SIZE")
            {
                var goldSize = (from item in _jewelryContext.TbmGoldSize
                                where item.Id == request.Id
                                && item.Code == request.Code.ToUpper()
                                select item).SingleOrDefault();

                if (goldSize == null)
                {
                    throw new HandleException($"ไม่พอข้อมูลรหัส {request.Code.ToUpper()}");
                }

                _jewelryContext.TbmGoldSize.Remove(goldSize);
                await _jewelryContext.SaveChangesAsync();
            }

            else if (request.Type.ToUpper() == "PRODUCT-TYPE")
            {
                var productType = (from item in _jewelryContext.TbmProductType
                                   where item.Id == request.Id
                                   && item.Code == request.Code.ToUpper()
                                   select item).SingleOrDefault();

                if (productType == null)
                {
                    throw new HandleException($"ไม่พอข้อมูลรหัส {request.Code.ToUpper()}");
                }

                _jewelryContext.TbmProductType.Remove(productType);
                await _jewelryContext.SaveChangesAsync();
            }

            return "success";
        }

        // ------ Create ------ //
        public async Task<string> CreateMasterModel(CreateMasterModelRequest request)
        {
            if (request.Type.ToUpper() == "GEM")
            {
                var gem = (from item in _jewelryContext.TbmGem
                           where item.Code == request.Code.ToUpper()
                           select item).SingleOrDefault();

                if (gem != null)
                {
                    throw new HandleException($"มีข้อมูลรหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
                }

                var newGem = new TbmGem()
                {
                    Code = request.Code.ToUpper(),
                    NameEn = request.NameEn,
                    NameTh = request.NameTh,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,
                    IsActive = true,
                };

                _jewelryContext.TbmGem.Add(newGem);
                await _jewelryContext.SaveChangesAsync();
            }

            else if (request.Type.ToUpper() == "GEM-SHAPE")
            {
                var gemShape = (from item in _jewelryContext.TbmGemShape
                                where item.Code == request.Code.ToUpper()
                                select item).SingleOrDefault();

                if (gemShape != null)
                {
                    throw new HandleException($"มีข้อมูลรหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
                }

                var newGemShape = new TbmGemShape()
                {
                    Code = request.Code.ToUpper(),
                    NameEn = request.NameEn,
                    NameTh = request.NameTh,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,
                    IsActive = true,
                };

                _jewelryContext.TbmGemShape.Add(newGemShape);
                await _jewelryContext.SaveChangesAsync();
            }

            else if (request.Type.ToUpper() == "GOLD-SIZE")
            {
                var goldSize = (from item in _jewelryContext.TbmGoldSize
                                where item.Code == request.Code.ToUpper()
                                select item).SingleOrDefault();

                if (goldSize != null)
                {
                    throw new HandleException($"มีข้อมูลรหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
                }

                var newGoldSize = new TbmGoldSize()
                {
                    Code = request.Code.ToUpper(),
                    NameEn = request.NameEn,
                    NameTh = request.NameTh,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,
                    IsActive = true,
                };

                _jewelryContext.TbmGoldSize.Add(newGoldSize);
                await _jewelryContext.SaveChangesAsync();
            }

            else if (request.Type.ToUpper() == "PRODUCT-TYPE")
            {
                var productType = (from item in _jewelryContext.TbmProductType
                                   where item.Code == request.Code.ToUpper()
                                   select item).SingleOrDefault();

                if (productType != null)
                {
                    throw new HandleException($"มีข้อมูลรหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
                }

                var newProductType = new TbmProductType()
                {
                    Code = request.Code.ToUpper(),
                    NameEn = request.NameEn,
                    NameTh = request.NameTh,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,
                    IsActive = true,
                };

                _jewelryContext.TbmProductType.Add(newProductType);
                await _jewelryContext.SaveChangesAsync();
            }

            else if (request.Type.ToUpper() == "ZILL")
            {
                var zill = (from item in _jewelryContext.TbmZill
                            where item.Code == request.Code.ToUpper()
                            select item).SingleOrDefault();

                if (zill != null)
                {
                    throw new HandleException($"มีข้อมูลรหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
                }
                if (string.IsNullOrEmpty(request.GoldCode))
                {
                    throw new HandleException($"กรุณาระบุประเภททอง");
                }
                if (string.IsNullOrEmpty(request.GoldSizeCode))
                {
                    throw new HandleException($"กรุณาระบุเปอร์เซ็นทอง");
                }

                var newZill = new TbmZill()
                {
                    Code = request.Code.ToUpper(),
                    NameEn = request.NameEn,
                    NameTh = request.NameTh,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    IsActive = true,
                    Remark = request.Description,
                    GoldCode = request.GoldCode,
                    GoldSizeCode = request.GoldSizeCode
                };

                _jewelryContext.TbmZill.Add(newZill);
                await _jewelryContext.SaveChangesAsync();
            }

            return "success";
        }
    }
}
