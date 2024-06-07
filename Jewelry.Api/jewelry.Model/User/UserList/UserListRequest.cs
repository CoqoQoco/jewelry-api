using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.UserList
{
    public class UserListRequest : DataSourceRequest
    {
        public UserList Search { get; set; }
    }

    public class UserList
    {
        public int? Id { get; set; }
        public string? UserName { get; set; }
        public string? Text { get; set; }
    }

    public class UserGet
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }
}
