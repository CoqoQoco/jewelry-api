﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public string FirstNameTH { get; set; }
        public string LastNameTH { get; set; }
        public string PrefixTH { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
    }
}
