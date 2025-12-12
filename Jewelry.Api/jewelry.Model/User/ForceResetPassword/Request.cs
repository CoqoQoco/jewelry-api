using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.ForceResetPassword
{
    public class Request
    {
        public string TargetUsername { get; set; }  // Username ของคนที่จะเปลี่ยน password
        public string NewPassword { get; set; }
    }
}
