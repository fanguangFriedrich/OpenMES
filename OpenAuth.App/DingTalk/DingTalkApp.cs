using Infrastructure;
using Microsoft.Extensions.Options;
using OpenAuth.App.DingTalk.Request;
using OpenAuth.Repository.Domain.DingTalk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAuth.App.DingTalk
{
    public class DingTalkApp
    {
        private readonly HttpClient _httpClient;
        private readonly DingTalkOptions _options;

        private string? _cachedCorpAccessToken;
        private DateTime _corpAccessTokenExpiry = DateTime.MinValue;

        // 用户授权码模式
        private const string TokenApiUrl = "https://api.dingtalk.com/v1.0/oauth2/userAccessToken";
        private const string UserInfoApiUrl = "https://api.dingtalk.com/v1.0/contact/users/me";

        // 企业内部应用模式（对应Java的 client_credentials）
        private const string CorpTokenApiUrl = "https://api.dingtalk.com/v1.0/oauth2/accessToken";
        private const string CorpUserInfoApiUrl = "https://oapi.dingtalk.com/topapi/v2/user/getuserinfo";
        private const string UserGetApiUrl = "https://oapi.dingtalk.com/topapi/v2/user/get";

        private const string UserListApiUrl = "https://oapi.dingtalk.com/topapi/v2/user/list";
        private const string DeptListSubApiUrl = "https://oapi.dingtalk.com/topapi/v2/department/listsub";
        private const string DeptGetApiUrl = "https://oapi.dingtalk.com/topapi/v2/department/get";

        public DingTalkApp(HttpClient httpClient, IOptions<DingTalkOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        private async Task<string> GetValidCorpAccessTokenAsync()
        {
            // 提前5分钟刷新，避免临界点过期
            if (_cachedCorpAccessToken != null && DateTime.UtcNow < _corpAccessTokenExpiry.AddMinutes(-5))
            {
                return _cachedCorpAccessToken;
            }

            var corpId = _options.CorpId;
            (_cachedCorpAccessToken, _corpAccessTokenExpiry) = await GetCorpAccessTokenAsync(corpId);

            Console.WriteLine($"[DingTalk] AccessToken 已刷新，有效期至: {_corpAccessTokenExpiry:yyyy-MM-dd HH:mm:ss} UTC");

            return _cachedCorpAccessToken;
        }

        /// <summary>
        /// 通过授权码获取用户信息
        /// </summary>
        public async Task<DingTalkUserInfo> GetUserByAuthCodeAsync(string authCode)
        {
            var accessToken = await GetAccessTokenAsync(authCode);
            return await GetUserInfoAsync(accessToken);
        }

        /// <summary>
        /// 通过授权码获取 AccessToken
        /// </summary>
        public async Task<string> GetAccessTokenAsync(string authCode)
        {
            var body = new
            {
                clientId = _options.ClientId,
                clientSecret = _options.ClientSecret,
                code = authCode,
                grantType = "authorization_code"
            };

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(TokenApiUrl, content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<DingTalkTokenResponse>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                throw new Exception("获取钉钉 AccessToken 失败");

            return tokenResponse.AccessToken;
        }

        /// <summary>
        /// 通过 AccessToken 获取用户信息
        /// </summary>
        public async Task<DingTalkUserInfo> GetUserInfoAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, UserInfoApiUrl);
            request.Headers.Add("x-acs-dingtalk-access-token", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<DingTalkUserInfo>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userInfo == null)
                throw new Exception("获取钉钉用户信息失败");

            return userInfo;
        }

        // ─────────────────────────────────────────────
        // 企业内部应用模式（对应 Java Controller 逻辑）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 通过免登授权码 获取用户信息（企业内部应用模式）
        /// 对应 Java: GET /api/getUserInfo?code=xxx&corpId=xxx
        /// </summary>
        public async Task<DingTalkUserInfo> GetUserByCorpCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code 不能为空", nameof(code));

            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            return await GetCorpUserInfoAsync(code, corpAccessToken);
        }

        /// <summary>
        /// 通过 client_credentials 模式获取企业 AccessToken
        /// 对应 Java: getAccessToken(clientId, clientSecret, corpId)
        /// </summary>
        public async Task<(string Token, DateTime Expiry)> GetCorpAccessTokenAsync(string corpId)
        {
            var body = new
            {
                appKey = _options.ClientId,
                appSecret = _options.ClientSecret,
                corpId = corpId,
                grantType = "client_credentials"
            };
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Remove("x-acs-dingtalk-access-token");
            var response = await _httpClient.PostAsync(CorpTokenApiUrl, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] 响应体: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取企业 AccessToken 失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("accessToken", out var tokenProp))
                throw new Exception($"响应中无 accessToken 字段: {resultJson}");

            if (!root.TryGetProperty("expireIn", out var expireInProp))
                throw new Exception($"响应中无 expireIn 字段: {resultJson}");

            var token = tokenProp.GetString() ?? throw new Exception("accessToken 为空");
            // 直接用接口返回的有效秒数
            var expiry = DateTime.UtcNow.AddSeconds(expireInProp.GetInt32());

            return (token, expiry);
        }

        /// <summary>
        /// 通过免登码 + 企业 AccessToken 查询用户信息
        /// 对应 Java: OapiV2UserGetuserinfoRequest / topapi/v2/user/getuserinfo
        /// </summary>
        public async Task<DingTalkUserInfo> GetCorpUserInfoAsync(string code, string corpAccessToken)
        {
            // 钉钉旧版 API 使用 POST + access_token 作为 query 参数
            var url = $"{CorpUserInfoApiUrl}?access_token={Uri.EscapeDataString(corpAccessToken)}";

            var body = new { code = code };
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DingTalkCorpUserInfoResponse>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null || result.Errcode != 0)
                throw new Exception($"获取企业用户信息失败: {result?.Errmsg}");

            return new DingTalkUserInfo
            {
                UnionId = result.Result?.UnionId,
                Nick=result.Result?.Name
            };
        }

        public async Task<List<long>> GetSubDeptIdListAsync(long deptId)
        {
            var corpAccessToken = await GetValidCorpAccessTokenAsync();

            var body = new
            {
                dept_id = deptId
            };

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 设置 access_token 到 Query 参数
            var url = $"https://oapi.dingtalk.com/topapi/v2/department/listsubid?access_token={corpAccessToken}";

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetSubDeptIdList 响应体: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取子部门ID列表失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("errcode", out var errcodeProp) || errcodeProp.GetInt32() != 0)
            {
                var errmsg = root.TryGetProperty("errmsg", out var errmsgProp) ? errmsgProp.GetString() : "未知错误";
                throw new Exception($"获取子部门ID列表失败: {errmsg}，响应: {resultJson}");
            }

            if (!root.TryGetProperty("result", out var resultProp))
                throw new Exception($"响应中无 result 字段: {resultJson}");

            if (!resultProp.TryGetProperty("dept_id_list", out var deptIdListProp))
                throw new Exception($"result 中无 dept_id_list 字段: {resultJson}");

            var deptIdList = new List<long>();
            foreach (var item in deptIdListProp.EnumerateArray())
            {
                deptIdList.Add(item.GetInt64());
            }

            return deptIdList;
        }

        public async Task TraverseDeptAsync(long deptId, List<long> allDeptIds)
        {
            var subDeptIds = await GetSubDeptIdListAsync(deptId);
            if (subDeptIds == null || subDeptIds.Count == 0)
                return;

            foreach (var subDeptId in subDeptIds)
            {
                allDeptIds.Add(subDeptId);
                // 递归获取子部门的子部门
                await TraverseDeptAsync(subDeptId, allDeptIds);
            }
        }

        /// <summary>
        /// 获取指定部门下所有员工信息（自动翻页）
        /// </summary>
        public async Task<List<DingTalkUserDetailInfo>> GetDeptUserListAsync(long deptId)
        {
            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{UserListApiUrl}?access_token={corpAccessToken}";

            var allUsers = new List<DingTalkUserDetailInfo>();
            long cursor = 0;
            const int size = 50;
            bool hasMore = true;

            while (hasMore)
            {
                var body = new
                {
                    dept_id = deptId,
                    cursor = cursor,
                    size = size,
                    contain_access_limit = false,
                    language = "zh_CN"
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var resultJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DingTalk] GetDeptUserList deptId={deptId} cursor={cursor} 响应: {resultJson}");

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"获取部门员工列表失败 [{response.StatusCode}]: {resultJson}");

                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;

                var errcode = root.GetProperty("errcode").GetInt32();
                if (errcode != 0)
                {
                    var errmsg = root.GetProperty("errmsg").GetString();
                    throw new Exception($"获取部门员工列表失败: {errmsg}");
                }

                var result = root.GetProperty("result");

                // ✅ 先取 has_more
                hasMore = result.GetProperty("has_more").GetBoolean();

                // ✅ 只有 has_more=true 时才取 next_cursor
                if (hasMore && result.TryGetProperty("next_cursor", out var nextCursorProp))
                {
                    cursor = nextCursorProp.GetInt64();
                }
                else
                {
                    hasMore = false;
                }

                // 解析 list
                if (result.TryGetProperty("list", out var list))
                {
                    if (list.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in list.EnumerateArray())
                        {
                            allUsers.Add(ParseUser(item));
                        }
                    }
                    else if (list.ValueKind == JsonValueKind.Object)
                    {
                        allUsers.Add(ParseUser(list));
                    }
                }

                if (hasMore)
                    await Task.Delay(100);
            }

            return allUsers;
        }

        /// <summary>
        /// 获取企业所有部门下的所有员工（去重）
        /// </summary>
        public async Task<List<DingTalkUserDetailInfo>> GetAllUsersAsync()
        {
            // 先获取所有部门ID
            var allDeptIds = new List<long>();
            await TraverseDeptAsync(1, allDeptIds);

            var userDict = new Dictionary<string, DingTalkUserDetailInfo>();

            foreach (var deptId in allDeptIds)
            {
                var users = await GetDeptUserListAsync(deptId);
                foreach (var user in users)
                {
                    // 以 userId 去重，同一员工可能属于多个部门
                    if (user.UserId != null && !userDict.ContainsKey(user.UserId))
                    {
                        userDict[user.UserId] = user;
                    }
                }
                await Task.Delay(100);
            }

            return userDict.Values.ToList();
        }

        private DingTalkUserDetailInfo ParseUser(JsonElement item)
        {
            return new DingTalkUserDetailInfo
            {
                UserId = item.TryGetProperty("userid", out var p) ? p.GetString() : null,
                Name = item.TryGetProperty("name", out p) ? p.GetString() : null,
                Mobile = item.TryGetProperty("mobile", out p) ? p.GetString() : null,
                Email = item.TryGetProperty("email", out p) ? p.GetString() : null,
                OrgEmail = item.TryGetProperty("org_email", out p) ? p.GetString() : null,
                Title = item.TryGetProperty("title", out p) ? p.GetString() : null,
                JobNumber = item.TryGetProperty("job_number", out p) ? p.GetString() : null,
                WorkPlace = item.TryGetProperty("work_place", out p) ? p.GetString() : null,
                Avatar = item.TryGetProperty("avatar", out p) ? p.GetString() : null,
                UnionId = item.TryGetProperty("unionid", out p) ? p.GetString() : null,
                Remark = item.TryGetProperty("remark", out p) ? p.GetString() : null,
                Telephone = item.TryGetProperty("telephone", out p) ? p.GetString() : null,
                StateCode = item.TryGetProperty("state_code", out p) ? p.GetString() : null,
                Extension = item.TryGetProperty("extension", out p) ? p.GetString() : null,
                HiredDate = item.TryGetProperty("hired_date", out p) ? p.GetInt64() : 0,
                Leader = item.TryGetProperty("leader", out p) && p.GetBoolean(),
                Boss = item.TryGetProperty("boss", out p) && p.GetBoolean(),
                Admin = item.TryGetProperty("admin", out p) && p.GetBoolean(),
                Active = item.TryGetProperty("active", out p) && p.GetBoolean(),
                ExclusiveAccount = item.TryGetProperty("exclusive_account", out p) && p.GetBoolean(),
                HideMobile = item.TryGetProperty("hide_mobile", out p) && p.GetBoolean(),
                DeptIdList = item.TryGetProperty("dept_id_list", out p)
                    ? p.EnumerateArray().Select(x => x.GetInt64()).ToList()
                    : null,
                DeptOrder = item.TryGetProperty("dept_order", out p) ? p.GetInt64() : 0,
            };
        }

        /// <summary>
        /// 通过 userId 获取用户详细信息
        /// </summary>
        public async Task<DingTalkUserDetailInfo> GetUserByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId 不能为空", nameof(userId));

            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{UserGetApiUrl}?access_token={corpAccessToken}";

            var body = new
            {
                userid = userId,
                language = "zh_CN"
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetUserByUserId userId={userId} 响应: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取用户详情失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            var errcode = root.GetProperty("errcode").GetInt32();
            if (errcode != 0)
            {
                var errmsg = root.GetProperty("errmsg").GetString();
                throw new Exception($"获取用户详情失败: {errmsg}");
            }

            var result = root.GetProperty("result");
            return ParseUser(result);
        }

        /// <summary>
        /// 获取指定父部门下的子部门列表（含部门详情）
        /// </summary>
        public async Task<List<DingTalkDeptInfo>> GetSubDeptListAsync(long? deptId = null)
        {
            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{DeptListSubApiUrl}?access_token={corpAccessToken}";

            var bodyObj = new Dictionary<string, object>
            {
                { "language", "zh_CN" }
            };
            if (deptId.HasValue)
                bodyObj["dept_id"] = deptId.Value;

            var json = JsonSerializer.Serialize(bodyObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetSubDeptList deptId={deptId} 响应: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取子部门列表失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            var errcode = root.GetProperty("errcode").GetInt32();
            if (errcode != 0)
            {
                var errmsg = root.GetProperty("errmsg").GetString();
                throw new Exception($"获取子部门列表失败: {errmsg}");
            }

            var result = root.GetProperty("result");
            var deptList = new List<DingTalkDeptInfo>();

            foreach (var item in result.EnumerateArray())
            {
                deptList.Add(new DingTalkDeptInfo
                {
                    DeptId = item.GetProperty("dept_id").GetInt64(),
                    Name = item.TryGetProperty("name", out var p) ? p.GetString() : null,
                    ParentId = item.TryGetProperty("parent_id", out p) ? p.GetInt64() : 0,
                    AutoAddUser = item.TryGetProperty("auto_add_user", out p) && p.GetBoolean(),
                    CreateDeptGroup = item.TryGetProperty("create_dept_group", out p) && p.GetBoolean(),
                });
            }

            return deptList;
        }

        /// <summary>
        /// 递归获取企业所有部门列表（含详情）
        /// </summary>
        public async Task<List<DingTalkDeptInfo>> GetAllDeptListAsync()
        {
            var allDepts = new List<DingTalkDeptInfo>();
            await TraverseDeptListAsync(1, allDepts);
            return allDepts;
        }

        private async Task TraverseDeptListAsync(long deptId, List<DingTalkDeptInfo> allDepts)
        {
            var subDepts = await GetSubDeptListAsync(deptId);
            if (subDepts == null || subDepts.Count == 0)
                return;

            foreach (var dept in subDepts)
            {
                allDepts.Add(dept);
                await Task.Delay(100);
                await TraverseDeptListAsync(dept.DeptId, allDepts);
            }
        }

        public async Task<DingTalkDeptInfo> GetDeptByIdAsync(long deptId)
        {
            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{DeptGetApiUrl}?access_token={corpAccessToken}";

            var body = new { dept_id = deptId, language = "zh_CN" };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetDeptById deptId={deptId} 响应: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取部门详情失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            var errcode = root.GetProperty("errcode").GetInt32();
            if (errcode != 0)
                throw new Exception($"获取部门详情失败: {root.GetProperty("errmsg").GetString()}");

            var result = root.GetProperty("result");
            return new DingTalkDeptInfo
            {
                DeptId = result.GetProperty("dept_id").GetInt64(),
                Name = result.TryGetProperty("name", out var p) ? p.GetString() : null,
                ParentId = result.TryGetProperty("parent_id", out p) ? p.GetInt64() : 0,
                AutoAddUser = result.TryGetProperty("auto_add_user", out p) && p.GetBoolean(),
                CreateDeptGroup = result.TryGetProperty("create_dept_group", out p) && p.GetBoolean(),
            };
        }
    }

    /// <summary>
    /// 钉钉 Token 接口响应
    /// </summary>
    internal class DingTalkTokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpireIn { get; set; }
    }

    /// <summary>
    /// topapi/v2/user/getuserinfo 外层响应
    /// </summary>
    internal class DingTalkCorpUserInfoResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [JsonPropertyName("errmsg")]
        public string Errmsg { get; set; }

        [JsonPropertyName("result")]
        public DingTalkCorpUserInfo Result { get; set; }
    }
}
