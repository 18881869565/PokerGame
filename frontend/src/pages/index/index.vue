<template>
  <view class="container">
    <view class="header">
      <text class="title">德州扑克</text>
      <text class="subtitle">好友对战 · 纯娱乐</text>
    </view>

    <view class="actions" v-if="!isLoggedIn">
      <button class="btn btn-primary" @click="goToLogin">登录 / 注册</button>
    </view>

    <view class="actions" v-else>
      <view class="user-info">
        <text class="nickname">{{ userStore.nickname }}</text>
        <text class="chips">筹码: {{ userStore.chips }}</text>
      </view>

      <button class="btn btn-primary" @click="createRoom">创建房间</button>
      <button class="btn btn-secondary" @click="joinRoom">加入房间</button>
      <button class="btn btn-outline" @click="goToLobby">进入大厅</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()

const isLoggedIn = computed(() => userStore.isLoggedIn)

const goToLogin = () => {
  uni.navigateTo({ url: '/pages/login/login' })
}

const createRoom = () => {
  uni.navigateTo({ url: '/pages/room/room?action=create' })
}

const joinRoom = () => {
  uni.navigateTo({ url: '/pages/room/room?action=join' })
}

const goToLobby = () => {
  uni.switchTab({ url: '/pages/lobby/lobby' })
}
</script>

<style scoped>
.container {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 100rpx 40rpx;
}

.header {
  text-align: center;
  margin-bottom: 80rpx;
}

.title {
  font-size: 72rpx;
  font-weight: bold;
  color: #e94560;
  display: block;
}

.subtitle {
  font-size: 28rpx;
  color: #999999;
  margin-top: 20rpx;
  display: block;
}

.actions {
  width: 100%;
  max-width: 600rpx;
}

.user-info {
  background: rgba(255, 255, 255, 0.1);
  border-radius: 20rpx;
  padding: 40rpx;
  text-align: center;
  margin-bottom: 40rpx;
}

.nickname {
  font-size: 36rpx;
  font-weight: bold;
  display: block;
}

.chips {
  font-size: 28rpx;
  color: #ffd700;
  margin-top: 10rpx;
  display: block;
}

.btn {
  width: 100%;
  height: 100rpx;
  line-height: 100rpx;
  border-radius: 50rpx;
  font-size: 32rpx;
  font-weight: bold;
  margin-bottom: 30rpx;
  border: none;
}

.btn-primary {
  background: linear-gradient(135deg, #e94560, #ff6b6b);
  color: #ffffff;
}

.btn-secondary {
  background: linear-gradient(135deg, #4a69bd, #6a89cc);
  color: #ffffff;
}

.btn-outline {
  background: transparent;
  color: #ffffff;
  border: 2rpx solid #ffffff;
}
</style>
