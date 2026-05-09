using Infrastructure.Cache;
using Jusoft.DingtalkStream.Core;
using OpenAuth.App.Request;
using OpenAuth.Repository.Domain.DingTalk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAuth.App.DingTalk
{
    public class DingtalkStreamMessageHandler : IDingtalkStreamMessageHandler
    {
        private readonly ICacheContext _cache;
        private readonly DingTalkApp _dingTalkApp;
        private readonly UserManagerApp _userManagerApp;
        private readonly OrgManagerApp _orgManagerApp;

        public DingtalkStreamMessageHandler(
            ICacheContext cache,
            DingTalkApp dingTalkApp,
            UserManagerApp userManagerApp,
            OrgManagerApp orgManagerApp)
        {
            _cache = cache;
            _dingTalkApp = dingTalkApp;
            _userManagerApp = userManagerApp;
            _orgManagerApp = orgManagerApp;
        }

        public async Task HandleMessage(MessageEventHanderArgs e)
        {
            var replyMessageData = string.Empty;

            switch (e.Type)
            {
                case SubscriptionType.EVENT:
                    {
                        var eventId = e.Headers.Payload.GetProperty("eventId").GetString();
                        var eventType = e.Headers.Payload.GetProperty("eventType").GetString();

                        // 去重
                        var cacheKey = $"dingtalk:event:{eventId}";
                        var existing = _cache.Get<string>(cacheKey);
                        if (existing != null)
                        {
                            replyMessageData = await DingtalkStreamUtilities
                                .CreateReply_EventSuccess_MessageData("success");
                            break;
                        }
                        _cache.Set(cacheKey, "1", DateTime.Now.AddHours(24));

                        switch (eventType)
                        {
                            case "user_add_org":
                                var addData = System.Text.Json.JsonSerializer
                                    .Deserialize<UserChangeEventData>(e.Data);
                                await HandleUserAdded(addData);
                                break;

                            case "user_leave_org":
                                var leaveData = System.Text.Json.JsonSerializer
                                    .Deserialize<UserChangeEventData>(e.Data);
                                await HandleUserLeave(leaveData);
                                break;

                            case "user_modify_org":
                                break;
                        }

                        replyMessageData = await DingtalkStreamUtilities
                            .CreateReply_EventSuccess_MessageData("success");
                        break;
                    }

                case SubscriptionType.CALLBACK:
                    replyMessageData = DingtalkStreamUtilities
                        .CreateReply_Callback_MessageData("自定义回调结果");
                    break;
            }

            var replyMessage = await DingtalkStreamUtilities
                .CreateReplyMessage(e.Headers.MessageId, replyMessageData);
            await e.Reply(replyMessage);
        }

        private async Task HandleUserAdded(UserChangeEventData data)
        {
            // 获取所有系统部门
            var existingOrgs = _orgManagerApp.LoadAll();

            foreach (var userId in data.UserId)
            {
                try
                {
                    // 1. 获取钉钉用户详情
                    var userDetail = await _dingTalkApp.GetUserByUserIdAsync(userId);
                    if (userDetail == null)
                    {
                        Console.WriteLine($"获取用户详情为空 userId={userId}");
                        continue;
                    }

                    // 2. 匹配部门：钉钉deptId → 钉钉部门名称 → 系统部门ID
                    var orgIdArr = new List<string>();
                    if (userDetail.DeptIdList != null && userDetail.DeptIdList.Count > 0)
                    {
                        foreach (var dingDeptId in userDetail.DeptIdList)
                        {
                            // 查钉钉部门详情拿部门名称
                            var dingDept = await _dingTalkApp.GetDeptByIdAsync(dingDeptId);
                            if (dingDept == null) continue;

                            // 用部门名称匹配系统部门
                            var sysOrg = existingOrgs.FirstOrDefault(o => o.Name == dingDept.Name);
                            if (sysOrg != null)
                            {
                                orgIdArr.Add(sysOrg.Id.ToString());
                            }
                        }
                    }

                    // 部门未匹配到则跳过
                    if (orgIdArr.Count == 0)
                    {
                        Console.WriteLine($"用户 {userDetail.Name} 未匹配到系统部门，跳过");
                        continue;
                    }

                    // 3. 构建请求，account = JobNumber_Name
                    var req = new UpdateUserReq
                    {
                        Id = null,
                        BizCode = userId,
                        Account = $"{userDetail.JobNumber}_{userDetail.Name}",
                        Name = userDetail.Name ?? string.Empty,
                        Password = userDetail.UnionId ?? string.Empty,
                        OrganizationIds = string.Join(",", orgIdArr),
                        ParentId = string.Empty,
                        Status = 0,
                        Sex = 0,
                    };

                    // 4. 新增用户
                    _userManagerApp.AddOrUpdateFromDingTalk(req, createId: "49df1602-f5f3-4d52-afb7-3802da619558");
                    Console.WriteLine($"新增用户成功: {userDetail.Name}, account: {req.Account}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理新增用户失败 userId={userId}: {ex.Message}");
                }
            }
        }

        private async Task HandleUserLeave(UserChangeEventData data)
        {
            foreach (var userId in data.UserId)
            {
                try
                {
                    _userManagerApp.DeleteByDingTalkUserId(userId);
                    Console.WriteLine($"删除用户成功 dingTalkUserId={userId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理离职用户失败 userId={userId}: {ex.Message}");
                }
            }
        }
    }

    public class UserChangeEventData
    {
        [JsonPropertyName("timeStamp")]
        public string TimeStamp { get; set; }

        [JsonPropertyName("eventId")]
        public string EventId { get; set; }

        [JsonPropertyName("optStaffId")]
        public string OptStaffId { get; set; }

        [JsonPropertyName("userId")]
        public List<string> UserId { get; set; }
    }
}
