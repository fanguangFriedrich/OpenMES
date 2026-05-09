import request from '@/utils/request'

export function dingTalkLogin(authCode) {
  console.log('DingTalk login with authCode:', authCode);
  return request({
    url: '/dingTalk/getUserByAuthCode',
    method: 'get',
    params: { authCode }
  })
}

export function loginByDingTalk(dingTalkUser) {
  return request({
    url: '/users/LoginByDingTalk',
    method: 'get',
    params: dingTalkUser  // dingTalkUser 是一个对象，包含 DingTalkUserInfo 的字段
  })
}

export function getUserByCorpCode(code) {
  return request({
    url: '/dingTalk/getUserByCorpCode',
    method: 'get',
    params: { code }
  })
}

// ─── 部门相关 ────────────────────────────────────────────

/**
 * 获取指定父部门下的子部门ID列表
 * @param {number} deptId 父部门ID，根部门传1
 */
export function getSubDeptIdList(deptId) {
  return request({
    url: '/DingTalk/GetSubDeptIdList',
    method: 'get',
    params: { deptId }
  })
}

/**
 * 获取企业所有部门ID列表（从根部门递归遍历）
 */
export function getAllDeptIdList() {
  return request({
    url: '/DingTalk/GetAllDeptIdList',
    method: 'get'
  })
}

/**
 * 获取指定父部门下的子部门列表（含部门详情）
 * @param {number} [deptId] 父部门ID，不传默认从根部门开始
 */
export function getSubDeptList(deptId) {
  return request({
    url: '/DingTalk/GetSubDeptList',
    method: 'get',
    params: deptId !== undefined ? { deptId } : {}
  })
}

/**
 * 递归获取企业所有部门列表（含详情）
 */
export function getAllDeptList() {
  return request({
    url: '/DingTalk/GetAllDeptList',
    method: 'get'
  })
}

// ─── 员工相关 ────────────────────────────────────────────
/**
 * 通过 userId 获取用户详细信息
 * @param {string} userId 钉钉用户userId
 */
export function getUserByUserId(userId) {
  return request({
    url: '/DingTalk/GetUserByUserId',
    method: 'get',
    params: { userId }
  })
}

/**
 * 获取企业所有员工列表（遍历全部部门，自动去重）
 */
export function getAllUsers() {
  return request({
    url: '/DingTalk/GetAllUsers',
    method: 'get'
  })
}

/**
 * 获取指定部门的员工列表（自动翻页，返回全量）
 * @param {number} deptId 部门ID
 */
export function getDeptUserList(deptId) {
  return request({
    url: '/DingTalk/GetDeptUserList',
    method: 'get',
    params: { deptId }
  })
}



/**
 * 获取企业 AccessToken（调试用）
 * @param {string} corpId 企业ID
 */
export function getCorpAccessToken(corpId) {
  return request({
    url: '/DingTalk/GetCorpAccessToken',
    method: 'get',
    params: { corpId }
  })
}

/**
 * 通过部门ID获取部门详细信息
 * @param {number} deptId 部门ID
 */
export function getDeptById(deptId) {
  return request({
    url: '/DingTalk/GetDeptById',
    method: 'get',
    params: { deptId }
  })
}