<template>
  <div class="login-container">
    <div class="login-box">
      <!-- 左侧插画/背景区 -->
      <div class="login-left">
        <img class="leftImg" src="~@/assets/login/bigLogo.png" alt="登录插画" />
      </div>

      <!-- 右侧表单区 -->
      <div class="login-form-wrap">
        <el-form
          class="login-form"
          autoComplete="on"
          :model="loginForm"
          :rules="loginRules"
          ref="loginForm"
          label-position="left"
        >
          <div class="form-header">
            <h3 class="title">俊杰机械 MES系统</h3>
            <p class="subtitle">欢迎回来，请登录您的账号</p>
          </div>

          <el-form-item prop="username">
            <el-input
              name="username"
              type="text"
              v-model="loginForm.username"
              autoComplete="on"
              placeholder="请输入登录账号"
            >
              <i
                slot="prefix"
                class="iconfont icon-yonghu_zhanghao_wode el-input__icon"
              ></i>
            </el-input>
          </el-form-item>

          <el-form-item prop="password">
            <el-input
              name="password"
              :type="pwdType"
              v-model="loginForm.password"
              autoComplete="on"
              placeholder="请输入密码"
            >
              <i slot="prefix" class="iconfont icon-mima el-input__icon"></i>
              <i
                slot="suffix"
                @click="showPwd"
                style="cursor: pointer"
                :class="
                  pwdType === 'password'
                    ? 'iconfont icon-yincang el-input__icon'
                    : 'iconfont icon-xianshi_chakan el-input__icon'
                "
              ></i>
            </el-input>
          </el-form-item>

          <el-form-item prop="code">
            <div class="code-wrapper">
              <el-input
                name="code"
                @keyup.enter.native="handleLogin"
                v-model="loginForm.code"
                autoComplete="off"
                placeholder="请输入验证码"
              >
                <i
                  slot="prefix"
                  class="iconfont icon-yanzhengma el-input__icon"
                ></i>
              </el-input>
              <identify
                v-model="identifyCode"
                class="imgCode"
                @click.native="changeCode(identifyCode)"
              ></identify>
            </div>
          </el-form-item>

          <el-form-item prop="tenantid">
            <el-select
              v-model="tenant"
              placeholder="请选择租户"
              @change="tenantChange"
              style="width: 100%"
            >
              <i
                slot="prefix"
                class="iconfont icon-yonghuguanli el-input__icon"
              ></i>
              <el-option
                v-for="item in tenants"
                :key="item.value"
                :label="item.label"
                :value="item.value"
              >
              </el-option>
            </el-select>
          </el-form-item>

          <div class="tips" v-if="isIdentityAuth">
            <router-link to="/oidcRedirect">
              <el-badge is-dot class="oauth-badge"
                >接口服务器启用了Oauth认证，请点击这里登录</el-badge
              >
            </router-link>
          </div>

          <div class="submit-btn-wrap" v-else>
            <el-button
              v-waves
              type="primary"
              class="login-btn"
              :loading="loading"
              @click.native.prevent="handleLogin"
            >
              登 录
            </el-button>
          </div>
        </el-form>
      </div>
    </div>
  </div>
</template>

<script>
import waves from "@/directive/waves";
import identify from "@/components/ImgVerify";
import { mapGetters, mapActions } from "vuex";

export default {
  name: "login",
  components: { identify },
  directives: { waves },
  data() {
    const validateUsername = (rule, value, callback) => {
      if (value.length <= 0) {
        callback(new Error("用户名不能为空"));
      } else {
        callback();
      }
    };
    const validatePass = (rule, value, callback) => {
      if (value.length <= 0) {
        callback(new Error("密码不能为空"));
      } else {
        callback();
      }
    };
    const validcode = (rule, value, callback) => {
      if (!value) {
        callback(new Error("验证码不能为空"));
      } else if (value.toLowerCase() !== this.identifyCode.toLowerCase()) {
        callback(new Error("验证码不正确"));
      } else {
        callback();
      }
    };
    return {
      tenant: this.tenantid || "OpenAuthDBContext",
      tenants: [
        { value: "OpenAuthDBContext", label: "默认租户" },
        { value: "ErrorId404", label: "模拟一个不存在的租户" },
      ],
      loginForm: {
        username: "System",
        password: "123456",
        code: "",
      },
      loginRules: {
        username: [
          { required: true, trigger: "blur", validator: validateUsername },
        ],
        password: [
          { required: true, trigger: "blur", validator: validatePass },
        ],
        code: [{ required: true, trigger: "blur", validator: validcode }],
      },
      loading: false,
      pwdType: "password",
      identifyCode: "",
    };
  },
  computed: {
    ...mapGetters(["isIdentityAuth", "tenantid"]),
  },
  methods: {
    ...mapActions(["setTenantId"]),
    tenantChange(e) {
      this.setTenantId(e);
    },
    showPwd() {
      this.pwdType = this.pwdType === "password" ? "" : "password";
    },
    handleLogin() {
      this.$refs.loginForm.validate((valid) => {
        if (valid) {
          this.loading = true;
          this.$store
            .dispatch("Login", this.loginForm)
            .then(() => {
              this.loading = false;
              this.$router.push({ path: "/" });
            })
            .catch(() => {
              this.loading = false;
            });
        } else {
          return false;
        }
      });
    },
    changeCode(val) {
      this.identifyCode = val;
    },
  },
};
</script>

<style rel="stylesheet/scss" lang="scss" scoped>
$theme-color: #3c3f4b;
$accent-color: #ff851b;
$bg-color: #f4f6f8;
$text-main: #3c3f4b;
$text-secondary: #7f8c9d;
$base-white: #ffffff;

.login-container {
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: $bg-color;

  &::before {
    content: none;
  }

  .login-box {
    position: relative;
    z-index: 2;
    display: flex;
    width: 1000px;
    height: 560px;
    background: $base-white;
    border-radius: 20px;
    box-shadow: 0 15px 40px rgba(60, 63, 75, 0.08);
    overflow: hidden;
    transition: all 0.3s ease;
  }

  .login-left {
    flex: 1;
    background: #fafbfd;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 40px;

    .leftImg {
      width: 100%;
      max-width: 450px;
      object-fit: contain;
      transition: transform 0.3s ease;
      &:hover {
        transform: scale(1.02);
      }
    }
  }

  .login-form-wrap {
    width: 450px;
    padding: 50px 60px;
    display: flex;
    flex-direction: column;
    justify-content: center;
    background: $base-white;

    .form-header {
      margin-bottom: 40px;
      .title {
        font-size: 28px;
        font-weight: 600;
        color: $text-main;
        margin: 0 0 10px 0;
        letter-spacing: 1px;
      }
      .subtitle {
        font-size: 14px;
        color: $text-secondary;
        margin: 0;
      }
    }

    .login-form {
      width: 100%;
    }

    ::v-deep .el-form-item {
      margin-bottom: 28px;
    }

    ::v-deep .el-input__inner {
      height: 48px;
      line-height: 48px;
      border-radius: 8px;
      border: 1px solid #e0e3e9;
      padding-left: 40px;
      font-size: 15px;
      color: $text-main;
      transition: all 0.3s;

      &:focus {
        border-color: $accent-color;
        box-shadow: 0 0 0 2px rgba(255, 133, 27, 0.1);
      }

      &::-webkit-input-placeholder {
        color: lighten($text-secondary, 10%);
      }
      &::-moz-placeholder {
        color: lighten($text-secondary, 10%);
      }
      &:-ms-input-placeholder {
        color: lighten($text-secondary, 10%);
      }
      &:-moz-placeholder {
        color: lighten($text-secondary, 10%);
      }
    }

    ::v-deep .el-input__icon {
      line-height: 48px;
      font-size: 18px;
      color: lighten($text-main, 15%);
    }

    ::v-deep .el-input__prefix {
      left: 10px;
    }

    .code-wrapper {
      display: flex;
      justify-content: space-between;
      align-items: center;

      .el-input {
        flex: 1;
        margin-right: 15px;
      }

      .imgCode {
        width: 120px;
        height: 48px;
        border-radius: 8px;
        cursor: pointer;
        overflow: hidden;
        border: 1px solid #e0e3e9;
        box-sizing: border-box;
      }
    }

    .tips {
      margin-bottom: 25px;
      font-size: 13px;
      .oauth-badge {
        color: $text-secondary;
        transition: color 0.3s;
        &:hover {
          color: $accent-color;
        }
      }
    }

    .submit-btn-wrap {
      margin-top: 10px;
      .login-btn {
        width: 100%;
        height: 50px;
        font-size: 18px;
        font-weight: 500;
        border-radius: 8px;
        background: $theme-color;
        border-color: $theme-color;
        color: $base-white;
        box-shadow: 0 5px 15px rgba(60, 63, 75, 0.2);
        transition: all 0.3s ease;

        &:hover {
          background: $accent-color;
          border-color: $accent-color;
          box-shadow: 0 6px 18px rgba(255, 133, 27, 0.3);
          transform: translateY(-1px);
        }
        &:active {
          transform: translateY(1px);
        }
      }
    }
  }
}

::v-deep .el-select .el-input__inner {
  padding-left: 40px !important;
}
::v-deep .el-select .el-input__prefix {
  left: 10px !important;
}
::v-deep .el-select-dropdown__item.selected {
  color: $accent-color !important;
}
::v-deep .el-select-dropdown__item.hover {
  background-color: rgba(255, 133, 27, 0.05) !important;
}

@media screen and (max-width: 1024px) {
  .login-container .login-box {
    width: 850px;
    height: 500px;
  }
  .login-container .login-form-wrap {
    padding: 40px;
  }
}

@media screen and (max-width: 768px) {
  .login-container .login-box {
    width: 90%;
    height: auto;
    flex-direction: column;
  }
  .login-container .login-left {
    display: none;
  }
  .login-container .login-form-wrap {
    width: 100%;
    padding: 40px 30px;
    box-sizing: border-box;
  }
}
</style>