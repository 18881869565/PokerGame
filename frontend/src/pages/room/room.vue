<template>
  <view class="container">
    <view class="room-header">
      <view class="room-info">
        <text class="room-code">房间号: {{ roomStore.roomCode }}</text>
        <text class="blinds">盲注: {{ roomStore.smallBlind }}/{{ roomStore.bigBlind }}</text>
      </view>
      <view class="qrcode-btn" @click="showQRCode">
        <text>分享二维码</text>
      </view>
    </view>

    <view class="players-grid">
      <view
        class="seat"
        v-for="i in roomStore.maxPlayers"
        :key="i"
        :class="{ occupied: getPlayerAtSeat(i - 1) }"
      >
        <template v-if="getPlayerAtSeat(i - 1)">
          <text class="player-name">{{ getPlayerAtSeat(i - 1)?.nickname }}</text>
          <text class="player-chips">{{ getPlayerAtSeat(i - 1)?.chips }}</text>
          <view class="ready-badge" v-if="getPlayerAtSeat(i - 1)?.isReady">
            <text>已准备</text>
          </view>
        </template>
        <template v-else>
          <text class="empty-seat">空位</text>
        </template>
      </view>
    </view>

    <view class="actions">
      <button class="btn btn-primary" @click="toggleReady" v-if="!isOwner">
        {{ isReady ? '取消准备' : '准备' }}
      </button>
      <button class="btn btn-primary" @click="startGame" v-if="isOwner">
        开始游戏
      </button>
      <button class="btn btn-outline" @click="leaveRoom">离开房间</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoomStore } from '@/stores/room'
import { useUserStore } from '@/stores/user'
import { roomApi } from '@/api'
import { useSignalR } from '@/composables/useSignalR'

const roomStore = useRoomStore()
const userStore = useUserStore()
const { connect, joinRoom: joinSignalR, leaveRoom: leaveSignalR, on } = useSignalR()

const isOwner = computed(() => roomStore.ownerId === userStore.userInfo?.id)
const isReady = computed(() => {
  const player = roomStore.players.find(p => p.userId === userStore.userInfo?.id)
  return player?.isReady || false
})

const getPlayerAtSeat = (seatIndex: number) => {
  return roomStore.players.find(p => p.seatIndex === seatIndex)
}

const toggleReady = async () => {
  // TODO: 调用准备/取消准备 API
}

const startGame = async () => {
  // TODO: 调用开始游戏 API
  uni.navigateTo({ url: '/pages/game/game' })
}

const showQRCode = async () => {
  try {
    const res = await roomApi.getQRCode(roomStore.roomCode)
    if (res.success && res.data) {
      // TODO: 显示二维码图片
      uni.showToast({ title: '二维码已生成', icon: 'success' })
    }
  } catch (e) {
    uni.showToast({ title: '生成二维码失败', icon: 'none' })
  }
}

const leaveRoom = async () => {
  await leaveSignalR(roomStore.roomCode)
  roomStore.clear()
  uni.navigateBack()
}

onMounted(async () => {
  const roomCode = uni.getStorageSync('currentRoomCode')
  if (roomCode) {
    try {
      const res = await roomApi.getRoom(roomCode)
      if (res.success && res.data) {
        roomStore.setRoom({
          roomCode: res.data.RoomCode,
          roomId: res.data.RoomId || 0,
          ownerId: res.data.OwnerId,
          maxPlayers: res.data.MaxPlayers,
          smallBlind: res.data.SmallBlind,
          bigBlind: res.data.BigBlind,
          status: res.data.Status
        })
        await connect()
        await joinSignalR(roomCode)
      }
    } catch (e) {
      uni.showToast({ title: '获取房间信息失败', icon: 'none' })
    }
  }
})
</script>

<style scoped>
.container {
  min-height: 100vh;
  padding: 40rpx;
}

.room-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 40rpx;
}

.room-code {
  font-size: 36rpx;
  font-weight: bold;
  color: #e94560;
}

.blinds {
  font-size: 28rpx;
  color: #999999;
  display: block;
  margin-top: 10rpx;
}

.qrcode-btn {
  padding: 20rpx 30rpx;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 10rpx;
}

.qrcode-btn text {
  color: #ffffff;
  font-size: 28rpx;
}

.players-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 20rpx;
  margin-bottom: 40rpx;
}

.seat {
  width: calc(33.33% - 14rpx);
  aspect-ratio: 1;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 20rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  position: relative;
}

.seat.occupied {
  background: rgba(233, 69, 96, 0.3);
}

.player-name {
  font-size: 28rpx;
  color: #ffffff;
  font-weight: bold;
}

.player-chips {
  font-size: 24rpx;
  color: #ffd700;
  margin-top: 10rpx;
}

.ready-badge {
  position: absolute;
  top: 10rpx;
  right: 10rpx;
  padding: 5rpx 15rpx;
  background: #4a69bd;
  border-radius: 10rpx;
}

.ready-badge text {
  font-size: 20rpx;
  color: #ffffff;
}

.empty-seat {
  font-size: 28rpx;
  color: #999999;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 20rpx;
}

.btn {
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

.btn-outline {
  background: transparent;
  color: #ffffff;
  border: 2rpx solid #ffffff;
}
</style>
