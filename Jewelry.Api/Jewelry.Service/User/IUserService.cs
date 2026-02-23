using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.User
{
    public interface IUserService
    {
        Task<jewelry.Model.User.Get.Response> Get();
        Task<jewelry.Model.User.GetAccount.Response> GetAccount(int id);
        Task<string> UpdateAccount(jewelry.Model.User.UpdateAccount.Request request);

        IQueryable<jewelry.Model.User.List.Response> List(jewelry.Model.User.List.Search request);
        Task<string> Create(jewelry.Model.User.Create.Request request);

        Task<string> Active(jewelry.Model.User.Active.Request request);
        Task<string> Inactive(jewelry.Model.User.Active.Request request);

        Task<string> ForceResetPassword(jewelry.Model.User.ForceResetPassword.Request request);

        IQueryable<jewelry.Model.User.ListMyjob.Response> ListMyJob(jewelry.Model.User.ListMyJob.Search request);
        Task<string> InactiveMyJob(jewelry.Model.User.InactiveMyJob.Request request);
    }
}
