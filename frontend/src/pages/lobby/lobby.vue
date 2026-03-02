<template>
  <view class="container">
    <view class="header">
      <text class="title">游戏大厅</text>
    </view>

    <view class="content">
      <view class="section">
        <text class="section-title">快速开始</text>
        <view class="quick-actions">
          <button class="btn btn-primary" @click="createRoom">创建房间</button>
          <button class="btn btn-secondary" @click="showJoinModal = true">加入房间</button>
        </view>
      </view>

      <view class="section">
        <text class="section-title">在线好友</text>
        <view class="empty-tip" v-if="onlineFriends.length === 0">
          <text>暂无在线好友</text>
        </view>
        <view class="friend-list" v-else>
          <view class="friend-item" v-for="friend in onlineFriends" :key="friend.id">
            <text class="friend-name">{{ friend.nickname }}</text>
            <button class="btn-small" @click="inviteFriend(friend)">邀请</button>
          </view>
        </view>
      </view>
    </view>

    <!-- 加入房间弹窗 -->
    <view class="modal" v-if="showJoinModal" @click.self="showJoinModal = false">
      <view class="modal-content">
        <text class="modal-title">加入房间</text>
        <input
          v-model="roomCode"
          class="input"
          placeholder="请输入6位房间号"
          maxlength="6"
          type="number"
        />
        <view class="modal-actions">
          <button class="btn btn-outline" @click="showJoinModal = false">取消</button>
          <button class="btn btn-primary" @click="joinRoom">确定</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { roomApi } from '@/api'

const showJoinModal = ref(false)
const roomCode = ref('')
const onlineFriends = ref<{ id: number; nickname: string }[]>([])

const createRoom = async () => {
  try {
    const res = await roomApi.create({
      maxPlayers: 9,
      smallBlind: 10,
      bigBlind: 20
    })
    if (res.success && res.data) {
      uni.navigateTo({ url: `/pages/room/room?roomCode=${res.data.roomCode}` })
    }
  } catch (e) {
    uni.showToast({ title: '创建房间失败', icon: 'none' })
  }
}

const joinRoom = async () => {
  if (!roomCode.value || roomCode.value.length !== 6) {
    uni.showToast({ title: '请输入6位房间号', icon: 'none' })
    return
  }
  uni.navigateTo({ url: `/pages/room/room?roomCode=${roomCode.value}` })
  showJoinModal.value = false
}

const inviteFriend = (friend: { id: number; nickname: string }) => {
  uni.showToast({ title: `已邀请 ${friend.nickname}`, icon: 'success' })
}
</script>

<style scoped>
.container {
  min-height: 100vh;
  padding: 40rpx;
}

.header {
  margin-bottom: 40rpx;
}

.title {
  font-size: 48rpx;
  font-weight: bold;
  color: #ffffff;
}

.section {
  margin-bottom: 60rpx;
}

.section-title {
  font-size: 32rpx;
  color: #999999;
  margin-bottom: 30rpx;
  display: block;
}

.quick-actions {
  display: flex;
  gap: 30rpx;
}

.btn {
  flex: 1;
  height: 100rpx;
  line-height: 100rpx;
  border-radius: 20rpx;
  font-size: 32rpx;
  font-weight: bold;
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

.empty-tip {
  text-align: center;
  padding: 60rpx;
  color: #999999;
}

.friend-list {
  background: rgba(255, 255, 255, 0.1);
  border-radius: 20rpx;
  overflow: hidden;
}

.friend-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 30rpx;
  border-bottom: 1rpx solid rgba(255, 255, 255, 0.1);
}

.friend-item:last-child {
  border-bottom: none;
}

.friend-name {
  font-size: 32rpx;
  color: #ffffff;
}

.btn-small {
  padding: 10rpx 30rpx;
  font-size: 28rpx;
  background: #e94560;
  color: #ffffff;
  border: none;
  border-radius: 10rpx;
}

.modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 999;
}

.modal-content {
  width: 80%;
  max-width: 600rpx;
  background: #1a1a2e;
  border-radius: 20rpx;
  padding: 40rpx;
}

.modal-title {
  font-size: 36rpx;
  font-weight: bold;
  color: #ffffff;
  text-align: center;
  display: block;
  margin-bottom: 40rpx;
}

.input {
  width: 100%;
  height: 100rpx;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 20rpx;
  padding: 0 30rpx;
  font-size: 36rpx;
  color: #ffffff;
  text-align: center;
  letter-spacing: 20rpx;
}

.modal-actions {
  display: flex;
  gap: 30rpx;
  margin-top: 40rpx;
}
</style>
