using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
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
        jewelry.Model.User.Get.Response Get();
    }
    public class UserService : BaseService, IUserService
    {
        private readonly JewelryContext _jewelryContext;
        public UserService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
        }

        public jewelry.Model.User.Get.Response Get()
        {
            var user = (from item in _jewelryContext.TbtUser
                        where item.Username == CurrentUsername
                        && item.Id == int.Parse(CurrentUserId)
                        select item).FirstOrDefault();

            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }

            return new jewelry.Model.User.Get.Response()
            {
                PrefixNameTh = user.PrefixNameTh,
                FirstNameTh = user.FirstNameTh,
                LastNameTh = user.LastNameTh,

                PermissionLevel = user.PermissionLevel
            };
        }
    }
}
