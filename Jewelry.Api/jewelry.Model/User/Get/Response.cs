using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.Get
{
    public class Response
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

    }

    public class Role
    { 
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
