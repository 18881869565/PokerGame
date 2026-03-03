<template>
  <view class="container">
    <view class="header">
      <text class="title">游戏大厅</text>
    </view>

    <view class="content">
      <view class="section">
        <text class="section-title">快速开始</text>
        <view class="quick-actions">
          <button class="btn btn-primary" @click="showCreateModal = true">创建房间</button>
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

    <!-- 创建房间弹窗 -->
    <view class="modal" v-if="showCreateModal" @click.self="showCreateModal = false">
      <view class="modal-content">
        <text class="modal-title">创建房间</text>
        <view class="chips-info">
          <text class="chips-label">当前筹码余额</text>
          <text class="chips-value">{{ userStore.userInfo?.chips || 0 }}</text>
        </view>
        <view class="input-row">
          <text class="input-label">带入筹码</text>
          <input
            v-model="createChips"
            class="input"
            placeholder="不填则默认1000"
            type="number"
          />
        </view>
        <text class="input-tip">最低入场筹码: {{ minChips }}（50个大盲）</text>
        <view class="modal-actions">
          <button class="btn btn-outline" @click="showCreateModal = false">取消</button>
          <button class="btn btn-primary" @click="createRoom">确定创建</button>
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
        <input
          v-model="joinChips"
          class="input"
          placeholder="带入筹码（最低1000）"
          type="number"
          style="margin-top: 20rpx;"
        />
        <text class="input-tip">当前筹码: {{ userStore.userInfo?.chips || 0 }}</text>
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
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()
const showCreateModal = ref(false)
const showJoinModal = ref(false)
const roomCode = ref('')
const createChips = ref('')
const joinChips = ref('')
const onlineFriends = ref<{ id: number; nickname: string }[]>([])

const minChips = 1000 // 最低入场筹码

const createRoom = async () => {
  const chips = parseInt(createChips.value) || 0

  if (chips > 0 && chips < minChips) {
    uni.showToast({ title: `最低入场筹码为${minChips}`, icon: 'none' })
    return
  }

  if (chips > (userStore.userInfo?.chips || 0)) {
    uni.showToast({ title: '筹码不足', icon: 'none' })
    return
  }

  try {
    const res = await roomApi.create({
      maxPlayers: 9,
      smallBlind: 10,
      bigBlind: 20,
      bringChips: chips
    })
    console.log('[Lobby] createRoom response:', res)
    if (res.success && res.data) {
      const data = res.data as any
      showCreateModal.value = false
      createChips.value = ''
      uni.navigateTo({ url: `/pages/room/room?roomCode=${data.roomCode}` })
    } else {
      uni.showToast({ title: res.message || '创建房间失败', icon: 'none' })
    }
  } catch (e: any) {
    console.error('[Lobby] createRoom error:', e)
    uni.showToast({ title: e.message || '创建房间失败', icon: 'none' })
  }
}

const joinRoom = async () => {
  if (!roomCode.value || roomCode.value.length !== 6) {
    uni.showToast({ title: '请输入6位房间号', icon: 'none' })
    return
  }

  const chips = parseInt(joinChips.value) || 0

  if (chips > 0 && chips < minChips) {
    uni.showToast({ title: `最低入场筹码为${minChips}`, icon: 'none' })
    return
  }

  if (chips > (userStore.userInfo?.chips || 0)) {
    uni.showToast({ title: '筹码不足', icon: 'none' })
    return
  }

  // 调用后端加入房间接口
  try {
    const code = roomCode.value  // 保存房间号，在清空之前
    const res = await roomApi.joinRoom(code, chips)
    if (res.success) {
      showJoinModal.value = false
      roomCode.value = ''
      joinChips.value = ''
      uni.navigateTo({ url: `/pages/room/room?roomCode=${code}` })
    } else {
      uni.showToast({ title: res.message || '加入房间失败', icon: 'none' })
    }
  } catch (e: any) {
    uni.showToast({ title: e.message || '加入房间失败', icon: 'none' })
  }
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

.input-tip {
  display: block;
  text-align: center;
  font-size: 24rpx;
  color: #999999;
  margin-top: 16rpx;
}

.chips-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 24rpx 30rpx;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 16rpx;
  margin-bottom: 30rpx;
}

.chips-label {
  font-size: 28rpx;
  color: #999999;
}

.chips-value {
  font-size: 36rpx;
  font-weight: bold;
  color: #ffd700;
}

.input-row {
  display: flex;
  align-items: center;
  margin-bottom: 16rpx;
}

.input-label {
  font-size: 28rpx;
  color: #ffffff;
  width: 160rpx;
  flex-shrink: 0;
}

.input-row .input {
  flex: 1;
  height: 80rpx;
  font-size: 32rpx;
  letter-spacing: normal;
}

.modal-actions {
  display: flex;
  gap: 30rpx;
  margin-top: 40rpx;
}
</style>
