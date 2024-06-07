using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.UserCreate
{
    public class UserCreateRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public string FirstNameTH { get; set; }
        public string LastNameTH { get; set; }
        public string PrefixTH { get; set; }

        public string Position { get; set; }
        public int Level { get; set; }
    }
}
