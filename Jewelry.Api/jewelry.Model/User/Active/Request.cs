using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.Active
{
    public class Request
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public bool IsNew { get; set; }

        public IEnumerable<Role>? Roles { get; set;}
    }
    public class Role
    {
        public int Id { get; set; }
    }
}
