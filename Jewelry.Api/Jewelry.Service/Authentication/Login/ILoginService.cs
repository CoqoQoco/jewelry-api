using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Authentication.Login
{
    public interface ILoginService
    {
        Task<jewelry.Model.Authentication.Login.Response> Login(jewelry.Model.Authentication.Login.Request request);
    }
}
