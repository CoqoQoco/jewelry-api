using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.User
{
    public interface IUserService
    {
        IQueryable<TbmAccount> GetUsers();
    }
    public class UserService : IUserService
    {
        private readonly JewelryContext _jewelryContext;
        public UserService(JewelryContext JewelryContext)
        {
            _jewelryContext = JewelryContext;

        }

        #region *** public Method ***
        public IQueryable<TbmAccount> GetUsers()
        {
            var query = (from item in _jewelryContext.TbmAccount
                         select item);

            return query;
        }
        #endregion
    }
}
