using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.UserActive
{
    public class UserActiveRequest
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        /// <summary>
        /// 1 = active, 0 = inactive
        /// </summary>
        public int Active { get; set; }
    }
}
