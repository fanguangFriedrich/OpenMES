using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.DingTalk
{
    public class DingTalkDeptInfo
    {
        public long DeptId { get; set; }
        public string? Name { get; set; }
        public long ParentId { get; set; }
        public bool AutoAddUser { get; set; }
        public bool CreateDeptGroup { get; set; }
    }
}
