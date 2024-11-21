using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.Create
{
    public class Request
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public List<Role>? Roles { get; set; }
    }

    public class Role 
    { 
        public int RoleId { get; set; }
    }
}
