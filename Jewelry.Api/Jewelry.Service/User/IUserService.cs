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
        jewelry.Model.User.GetAccount.Response GetAccount(int id);
        IQueryable<jewelry.Model.User.List.Response> List(jewelry.Model.User.List.Search request);
        Task<string> Create(jewelry.Model.User.Create.Request request);

        Task<string> Active(jewelry.Model.User.Active.Request request);
    }
}
