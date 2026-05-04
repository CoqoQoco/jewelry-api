using jewelry.Model.Master.Bank;
using Jewelry.Data.Context;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;

namespace Jewelry.Service.Master.Bank
{
    public interface IMasterBankService
    {
        IQueryable<BankResponse> GetBankList();
    }

    public class MasterBankService : BaseService, IMasterBankService
    {
        private readonly JewelryContext _jewelryContext;

        public MasterBankService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public IQueryable<BankResponse> GetBankList()
        {
            var query = from bank in _jewelryContext.TbmBank
                        where bank.IsActive == true
                        orderby bank.Code
                        select new BankResponse
                        {
                            Code = bank.Code,
                            NameTh = bank.NameTh,
                            NameEn = bank.NameEn
                        };

            return query;
        }
    }
}
