using global::OpenAuth.App.DingTalk;
using global::OpenAuth.Repository.Domain.DingTalk;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAuth.App;
using OpenAuth.App.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAuth.WebApi.Controllers
{
    /// <summary>
    /// 钉钉登录
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "钉钉_DingTalk")]
    public class DingTalkController : ControllerBase
    {
        private readonly DingTalkApp _app;

        public DingTalkController(DingTalkApp app)
        {
            _app = app;
        }

        // ─────────────────────────────────────────────
        // 用户授权码模式（原有接口）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 通过授权码获取用户信息
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkUserInfo>> GetUserByAuthCode([FromQuery] string authCode)
        {
            var result = new Response<DingTalkUserInfo>();
            try
            {
                result.Data = await _app.GetUserByAuthCodeAsync(authCode);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过授权码获取用户级 AccessToken
        /// </summary>
        [HttpGet]
        public async Task<Response<string>> GetAccessToken([FromQuery] string authCode)
        {
            var result = new Response<string>();
            try
            {
                result.Data = await _app.GetAccessTokenAsync(authCode);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过用户级 AccessToken 获取用户信息
        /// </summary>
        [HttpGet]
        public async Task<Response<DingTalkUserInfo>> GetUserInfo([FromQuery] string accessToken)
        {
            var result = new Response<DingTalkUserInfo>();
            try
            {
                result.Data = await _app.GetUserInfoAsync(accessToken);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        // ─────────────────────────────────────────────
        // 企业内部应用模式（新增接口）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 通过免登授权码获取企业用户信息（企业内部应用模式）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkUserInfo>> GetUserByCorpCode([FromQuery] string code)
        {
            var result = new Response<DingTalkUserInfo>();
            try
            {
                result.Data = await _app.GetUserByCorpCodeAsync(code);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过 corpId 获取企业级 AccessToken（client_credentials 模式）
        /// </summary>
        [HttpGet]
        public async Task<Response<string>> GetCorpAccessToken([FromQuery] string corpId)
        {
            var result = new Response<string>();
            try
            {
                var (token, _) = await _app.GetCorpAccessTokenAsync(corpId);
                result.Data = token;
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过免登码 + 企业 AccessToken 获取企业用户信息
        /// </summary>
        [HttpGet]
        public async Task<Response<DingTalkUserInfo>> GetCorpUserInfo(
            [FromQuery] string code,
            [FromQuery] string corpAccessToken)
        {
            var result = new Response<DingTalkUserInfo>();
            try
            {
                result.Data = await _app.GetCorpUserInfoAsync(code, corpAccessToken);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过 userId 获取用户详细信息
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkUserDetailInfo>> GetUserByUserId([FromQuery] string userId)
        {
            var result = new Response<DingTalkUserDetailInfo>();
            try
            {
                result.Data = await _app.GetUserByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取指定父部门下的子部门ID列表
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<long>>> GetSubDeptIdList([FromQuery] long deptId)
        {
            var result = new Response<List<long>>();
            try
            {
                result.Data = await _app.GetSubDeptIdListAsync(deptId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 递归获取企业下所有部门ID列表（从根部门开始）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<long>>> GetAllDeptIdList()
        {
            var result = new Response<List<long>>();
            try
            {
                var allDeptIds = new List<long>();
                await _app.TraverseDeptAsync(1, allDeptIds);
                result.Data = allDeptIds;
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取指定部门的员工列表
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<DingTalkUserDetailInfo>>> GetDeptUserList([FromQuery] long deptId)
        {
            var result = new Response<List<DingTalkUserDetailInfo>>();
            try
            {
                result.Data = await _app.GetDeptUserListAsync(deptId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取企业所有员工列表（遍历全部部门，自动去重）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<DingTalkUserDetailInfo>>> GetAllUsers()
        {
            var result = new Response<List<DingTalkUserDetailInfo>>();
            try
            {
                result.Data = await _app.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取指定父部门下的子部门列表（含部门详情），不传deptId默认从根部门开始
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<DingTalkDeptInfo>>> GetSubDeptList([FromQuery] long? deptId = null)
        {
            var result = new Response<List<DingTalkDeptInfo>>();
            try
            {
                result.Data = await _app.GetSubDeptListAsync(deptId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 递归获取企业所有部门列表（含详情）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<DingTalkDeptInfo>>> GetAllDeptList()
        {
            var result = new Response<List<DingTalkDeptInfo>>();
            try
            {
                result.Data = await _app.GetAllDeptListAsync();
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过部门ID获取部门详细信息
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkDeptInfo>> GetDeptById([FromQuery] long deptId)
        {
            var result = new Response<DingTalkDeptInfo>();
            try
            {
                result.Data = await _app.GetDeptByIdAsync(deptId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}