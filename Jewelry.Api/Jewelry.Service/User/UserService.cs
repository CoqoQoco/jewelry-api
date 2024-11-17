using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
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
     
    }
    public class UserService : IUserService
    {
        private readonly JewelryContext _jewelryContext;

        private readonly string _admin = "@ADMIN";
        public UserService(JewelryContext JewelryContext)
        {
            _jewelryContext = JewelryContext;
        }

        #region *** public Method ***
     
        #endregion
    }
}
