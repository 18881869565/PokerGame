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

      <button class="btn btn-gift" @click="claimDailyGift" :disabled="claimingGift">
        {{ claimingGift ? '领取中...' : '领取免费筹码 +10000' }}
      </button>

      <button class="btn btn-primary" @click="showCreateModal = true">创建房间</button>
      <button class="btn btn-secondary" @click="showJoinModal = true">加入房间</button>
      <button class="btn btn-outline" @click="goToLobby">进入大厅</button>
    </view>

    <!-- 创建房间弹窗 -->
    <view class="modal" v-if="showCreateModal" @click.self="showCreateModal = false">
      <view class="modal-content">
        <text class="modal-title">创建房间</text>
        <view class="chips-info">
          <text class="chips-label">当前筹码余额</text>
          <text class="chips-value">{{ userStore.chips }}</text>
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
        <text class="input-tip">当前筹码: {{ userStore.chips }}</text>
        <view class="modal-actions">
          <button class="btn btn-outline" @click="showJoinModal = false">取消</button>
          <button class="btn btn-primary" @click="joinRoomWithCode">确定</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useUserStore } from '@/stores/user'
import { roomApi, userApi } from '@/api'

const userStore = useUserStore()
const claimingGift = ref(false)
const showCreateModal = ref(false)
const showJoinModal = ref(false)
const createChips = ref('')
const joinChips = ref('')
const roomCode = ref('')

const minChips = 1000 // 最低入场筹码（50个大盲）

const isLoggedIn = computed(() => userStore.isLoggedIn)

const goToLogin = () => {
  uni.navigateTo({ url: '/pages/login/login' })
}

const claimDailyGift = async () => {
  if (claimingGift.value) return

  claimingGift.value = true
  try {
    const res = await userApi.dailyGift()
    if (res.success) {
      const data = res.data as any
      uni.showToast({ title: `获得 ${data?.giftAmount || 10000} 筹码！`, icon: 'success' })
      // 更新用户信息
      await userStore.fetchUserInfo()
    } else {
      uni.showToast({ title: res.message || '领取失败', icon: 'none' })
    }
  } catch (e: any) {
    uni.showToast({ title: e.message || '领取失败', icon: 'none' })
  } finally {
    claimingGift.value = false
  }
}

const createRoom = async () => {
  if (!isLoggedIn.value) {
    goToLogin()
    return
  }

  const chips = parseInt(createChips.value) || 0

  if (chips > 0 && chips < minChips) {
    uni.showToast({ title: `最低入场筹码为${minChips}`, icon: 'none' })
    return
  }

  if (chips > (userStore.chips || 0)) {
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
    console.log('[Index] createRoom response:', res)
    if (res.success && res.data) {
      const data = res.data as any
      showCreateModal.value = false
      createChips.value = ''
      uni.navigateTo({ url: `/pages/room/room?roomCode=${data.roomCode}` })
    } else {
      uni.showToast({ title: res.message || '创建房间失败', icon: 'none' })
    }
  } catch (e: any) {
    console.error('[Index] createRoom error:', e)
    uni.showToast({ title: e.message || '创建房间失败', icon: 'none' })
  }
}

const joinRoomWithCode = async () => {
  if (!roomCode.value || roomCode.value.length !== 6) {
    uni.showToast({ title: '请输入6位房间号', icon: 'none' })
    return
  }

  const chips = parseInt(joinChips.value) || 0

  if (chips > 0 && chips < minChips) {
    uni.showToast({ title: `最低入场筹码为${minChips}`, icon: 'none' })
    return
  }

  if (chips > (userStore.chips || 0)) {
    uni.showToast({ title: '筹码不足', icon: 'none' })
    return
  }

  try {
    const code = roomCode.value
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

.btn-gift {
  background: linear-gradient(135deg, #f39c12, #f1c40f);
  color: #ffffff;
}

.btn-gift[disabled] {
  opacity: 0.6;
}

.btn-outline {
  background: transparent;
  color: #ffffff;
  border: 2rpx solid #ffffff;
}

/* 弹窗样式 */
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

.input {
  width: 100%;
  height: 80rpx;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 16rpx;
  padding: 0 30rpx;
  font-size: 32rpx;
  color: #ffffff;
  text-align: center;
  box-sizing: border-box;
}

.input-row .input {
  flex: 1;
}

.input-tip {
  display: block;
  text-align: center;
  font-size: 24rpx;
  color: #999999;
  margin-top: 16rpx;
}

.modal-actions {
  display: flex;
  gap: 30rpx;
  margin-top: 40rpx;
}

.modal-actions .btn {
  flex: 1;
  margin-bottom: 0;
}
</style>
