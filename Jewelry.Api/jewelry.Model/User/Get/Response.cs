using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.Get
{
    public class Response
    {
        public string PrefixNameTh { get; set; }
        public string FirstNameTh { get; set; }
        public string LastNameTh { get; set; }

        public string PrefixNameEn { get; set; }
        public string FirstNameEn { get; set; }
        public string LastNameEN { get; set; }

    }

    public class Role
    { 
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
