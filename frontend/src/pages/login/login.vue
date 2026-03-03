<template>
  <view class="container">
    <view class="header">
      <text class="title">{{ isRegister ? '注册账号' : '登录' }}</text>
    </view>

    <view class="form">
      <view class="form-item">
        <input
          v-model="form.username"
          class="input"
          placeholder="请输入用户名"
          type="text"
        />
      </view>

      <view class="form-item">
        <input
          v-model="form.password"
          class="input"
          placeholder="请输入密码"
          :password="true"
        />
      </view>

      <view class="form-item" v-if="isRegister">
        <input
          v-model="form.nickname"
          class="input"
          placeholder="请输入昵称（选填）"
          type="text"
        />
      </view>

      <button class="btn btn-primary" @click="submit" :disabled="loading">
        {{ loading ? '处理中...' : (isRegister ? '注册' : '登录') }}
      </button>

      <view class="switch-mode" @click="toggleMode">
        <text>{{ isRegister ? '已有账号？去登录' : '没有账号？去注册' }}</text>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useUserStore } from '@/stores/user'
import { authApi } from '@/api'

const userStore = useUserStore()

const isRegister = ref(false)
const loading = ref(false)
const form = ref({
  username: '',
  password: '',
  nickname: ''
})

const toggleMode = () => {
  isRegister.value = !isRegister.value
}

const submit = async () => {
  if (!form.value.username || !form.value.password) {
    uni.showToast({ title: '请填写完整信息', icon: 'none' })
    return
  }

  loading.value = true

  try {
    if (isRegister.value) {
      // 注册
      const regRes = await authApi.register({
        username: form.value.username,
        password: form.value.password,
        nickname: form.value.nickname || form.value.username
      })
      if (regRes.success) {
        uni.showToast({ title: '注册成功，请登录', icon: 'success' })
        isRegister.value = false
      } else {
        uni.showToast({ title: regRes.message || '注册失败', icon: 'none' })
      }
    } else {
      // 登录
      const loginRes = await authApi.login({
        username: form.value.username,
        password: form.value.password
      })
      if (loginRes.success && loginRes.data) {
        const { token, user } = loginRes.data as any
        userStore.setToken(token)
        userStore.setUser({
          id: user.id,
          username: user.username,
          nickname: user.nickname || user.username,
          chips: user.chips || 10000,
          avatar: user.avatar
        })
        uni.showToast({ title: '登录成功', icon: 'success' })
        setTimeout(() => {
          uni.switchTab({ url: '/pages/index/index' })
        }, 1000)
      } else {
        uni.showToast({ title: loginRes.message || '登录失败', icon: 'none' })
      }
    }
  } catch (error: any) {
    uni.showToast({ title: error.message || '操作失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.container {
  min-height: 100vh;
  padding: 100rpx 40rpx;
}

.header {
  text-align: center;
  margin-bottom: 80rpx;
}

.title {
  font-size: 48rpx;
  font-weight: bold;
  color: #ffffff;
}

.form {
  max-width: 600rpx;
  margin: 0 auto;
}

.form-item {
  margin-bottom: 40rpx;
}

.input {
  width: 100%;
  height: 100rpx;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 20rpx;
  padding: 0 30rpx;
  font-size: 32rpx;
  color: #ffffff;
}

.btn {
  width: 100%;
  height: 100rpx;
  line-height: 100rpx;
  border-radius: 50rpx;
  font-size: 32rpx;
  font-weight: bold;
  margin-top: 40rpx;
}

.btn-primary {
  background: linear-gradient(135deg, #e94560, #ff6b6b);
  color: #ffffff;
}

.switch-mode {
  text-align: center;
  margin-top: 40rpx;
  color: #999999;
  font-size: 28rpx;
}
</style>
