using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.GetAccount
{
    public class Response
    {
        public int Id { get; set; }
        public string Username { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public bool IsActive { get; set; }
        public bool IsNew { get; set; }

        public DateTime? LastLogin { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        public string? Image { get; set; }

        public IEnumerable<Role>? Roles { get; set; }
        public IEnumerable<MasterRole>? MasterRoles { get; set; }
    }

    public class Role
    { 
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public class MasterRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
