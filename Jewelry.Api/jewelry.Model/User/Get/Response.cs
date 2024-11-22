using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.Get
{
    public class Response
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public IEnumerable<Role> Role { get; set; }

    }

    public class Role
    {
        public int Id { get; set; }
        public int Level { get; set; } 
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
