using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.DingTalk
{
    public class DingTalkUserDetailInfo
    {
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? OrgEmail { get; set; }
        public string? Title { get; set; }
        public string? JobNumber { get; set; }
        public string? WorkPlace { get; set; }
        public string? Avatar { get; set; }
        public string? UnionId { get; set; }
        public string? Remark { get; set; }
        public string? Telephone { get; set; }
        public string? StateCode { get; set; }
        public string? Extension { get; set; }
        public long HiredDate { get; set; }
        public bool Leader { get; set; }
        public bool Boss { get; set; }
        public bool Admin { get; set; }
        public bool Active { get; set; }
        public bool ExclusiveAccount { get; set; }
        public bool HideMobile { get; set; }
        public List<long>? DeptIdList { get; set; }
        public long DeptOrder { get; set; }
    }
}
