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
        Task<string> Create(jewelry.Model.User.Create.Request request);
    }
}
