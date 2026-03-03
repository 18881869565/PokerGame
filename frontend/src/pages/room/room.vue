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
        :class="{
          occupied: getPlayerAtSeat(i - 1),
          clickable: canClickSeat(i - 1),
          current: isCurrentPlayerSeat(i - 1)
        }"
        @click="handleSeatClick(i - 1)"
      >
        <template v-if="getPlayerAtSeat(i - 1)">
          <view class="owner-badge" v-if="getPlayerAtSeat(i - 1)?.isOwner">
            <text>房主</text>
          </view>
          <text class="player-name">{{ getPlayerAtSeat(i - 1)?.nickname }}</text>
          <text class="player-chips">{{ getPlayerAtSeat(i - 1)?.chips }}</text>
          <view class="ready-badge" v-if="getPlayerAtSeat(i - 1)?.isReady">
            <text>已准备</text>
          </view>
        </template>
        <template v-else>
          <text class="empty-seat">{{ isSeated ? '空位' : '点击入座' }}</text>
        </template>
      </view>
    </view>

    <view class="actions">
      <template v-if="!isSeated">
        <!-- 未入座：显示入座提示 -->
        <view class="tip-text">
          <text>请点击空位入座</text>
        </view>
      </template>
      <template v-else>
        <!-- 已入座：显示准备按钮 -->
        <button
          class="btn"
          :class="isReady ? 'btn-success' : 'btn-primary'"
          @click="toggleReady"
          v-if="!isOwner"
        >
          {{ isReady ? '已准备 (点击取消)' : '准备' }}
        </button>
        <button class="btn btn-primary" @click="startGame" v-if="isOwner">
          开始游戏
        </button>
      </template>
      <button class="btn btn-outline" @click="leaveRoom">离开房间</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue'
import { onLoad, onUnload } from '@dcloudio/uni-app'
import { useRoomStore } from '@/stores/room'
import { useUserStore } from '@/stores/user'
import { roomApi } from '@/api'
import { useSignalR } from '@/composables/useSignalR'

const roomStore = useRoomStore()
const userStore = useUserStore()
const signalR = useSignalR()
const { connect, joinRoom: joinSignalR, leaveRoom: leaveSignalR, readyGame, startGame: startGameSignalR, connection } = signalR

// 当前房间号（从 URL 参数或 storage 获取）
const currentRoomCode = ref('')

const isOwner = computed(() => roomStore.ownerId === userStore.userInfo?.id)

// 当前用户是否已入座（seatIndex >= 0）
const isSeated = computed(() => {
  const player = roomStore.players.find(p => p.userId === userStore.userInfo?.id)
  return player !== undefined && player.seatIndex >= 0
})

const isReady = computed(() => {
  const player = roomStore.players.find(p => p.userId === userStore.userInfo?.id)
  return player?.isReady || false
})

const getPlayerAtSeat = (seatIndex: number) => {
  // 只返回已入座的玩家（seatIndex >= 0）
  return roomStore.players.find(p => p.seatIndex === seatIndex && p.seatIndex >= 0)
}

// 是否是当前玩家的座位
const isCurrentPlayerSeat = (seatIndex: number) => {
  const currentPlayer = roomStore.players.find(p => p.userId === userStore.userInfo?.id)
  return currentPlayer?.seatIndex === seatIndex && seatIndex >= 0
}

// 判断座位是否可以点击
const canClickSeat = (seatIndex: number) => {
  const player = getPlayerAtSeat(seatIndex)
  // 空位可以点击
  if (!player) return true
  // 自己的座位可以点击（用于切换位置）
  if (player.userId === userStore.userInfo?.id) return false
  return false
}

// 处理座位点击
const handleSeatClick = async (seatIndex: number) => {
  const player = getPlayerAtSeat(seatIndex)

  if (!player) {
    // 点击空位：入座或换位置
    if (isSeated.value) {
      // 已入座，换位置
      await changeSeat(seatIndex)
    } else {
      // 未入座，入座
      await takeSeat(seatIndex)
    }
  }
}

// 入座
const takeSeat = async (seatIndex: number) => {
  const roomId = roomStore.roomId
  if (!roomId) {
    uni.showToast({ title: '房间信息丢失', icon: 'none' })
    return
  }

  try {
    const res = await roomApi.changeSeat(roomId, seatIndex)
    if (res.success) {
      await refreshRoom()
    } else {
      uni.showToast({ title: res.message || '入座失败', icon: 'none' })
    }
  } catch (e: any) {
    uni.showToast({ title: e.message || '入座失败', icon: 'none' })
  }
}

// 换位置
const changeSeat = async (newSeatIndex: number) => {
  const roomId = roomStore.roomId
  if (!roomId) {
    uni.showToast({ title: '房间信息丢失', icon: 'none' })
    return
  }

  try {
    const res = await roomApi.changeSeat(roomId, newSeatIndex)
    if (res.success) {
      uni.showToast({ title: '换位置成功', icon: 'success' })
      await refreshRoom()
    } else {
      uni.showToast({ title: res.message || '换位置失败', icon: 'none' })
    }
  } catch (e: any) {
    uni.showToast({ title: e.message || '换位置失败', icon: 'none' })
  }
}

const toggleReady = async () => {
  const code = currentRoomCode.value || roomStore.roomCode
  if (!code) {
    uni.showToast({ title: '房间信息丢失', icon: 'none' })
    return
  }
  await readyGame(code)
  // 立即刷新房间信息
  await refreshRoom()
}

const startGame = async () => {
  const code = currentRoomCode.value || roomStore.roomCode
  if (!code) {
    uni.showToast({ title: '房间信息丢失', icon: 'none' })
    return
  }
  // 调用 SignalR 开始游戏（会通知所有玩家）
  await startGameSignalR(code)
}

const showQRCode = async () => {
  try {
    const res = await roomApi.getQRCode(roomStore.roomCode)
    if (res.success && res.data) {
      const data = res.data as any
      uni.showModal({
        title: '房间邀请',
        content: `房间号: ${roomStore.roomCode}\n分享链接: ${data.qrCodeUrl || data.QRCodeUrl || ''}`,
        showCancel: false
      })
    }
  } catch (e: any) {
    uni.showToast({ title: e.message || '生成二维码失败', icon: 'none' })
  }
}

const leaveRoom = async () => {
  // 显示确认弹窗
  uni.showModal({
    title: '离开房间',
    content: '确定要离开房间吗？',
    confirmText: '确定离开',
    cancelText: '取消',
    success: async (res) => {
      if (res.confirm) {
        try {
          await leaveSignalR(roomStore.roomCode)
        } catch (e) {
          // ignore
        }
        roomStore.clear()
        uni.switchTab({ url: '/pages/lobby/lobby' })
      }
    }
  })
}

// 设置页面级别的事件监听
const setupPageEventListeners = () => {
  if (!connection.value) return

  // 这些是页面特有的逻辑，需要在这里处理
  connection.value.on('PlayerReady', () => {
    console.log('[Room] PlayerReady event received')
    refreshRoom()
  })
  connection.value.on('PlayerJoined', () => {
    console.log('[Room] PlayerJoined event received')
    refreshRoom()
  })
  connection.value.on('PlayerLeft', () => {
    console.log('[Room] PlayerLeft event received')
    refreshRoom()
  })
  connection.value.on('SeatChanged', () => {
    console.log('[Room] SeatChanged event received')
    refreshRoom()
  })
  connection.value.on('GameStarted', (data: any) => {
    console.log('[Room] GameStarted event received:', data)
    uni.showToast({ title: '游戏开始！', icon: 'success' })
    // 标记正在跳转到游戏页面，避免触发离开房间逻辑
    isNavigatingToGame.value = true
    // 跳转到游戏页面
    setTimeout(() => {
      uni.navigateTo({ url: '/pages/game/game?roomCode=' + roomStore.roomCode })
    }, 500)
  })
}

// 清理页面级别的事件监听
const cleanupPageEventListeners = () => {
  if (!connection.value) return

  connection.value.off('PlayerReady')
  connection.value.off('PlayerJoined')
  connection.value.off('PlayerLeft')
  connection.value.off('SeatChanged')
  connection.value.off('GameStarted')
}

// 刷新房间信息
const refreshRoom = async () => {
  const code = currentRoomCode.value || roomStore.roomCode
  if (!code) {
    console.error('[Room] 无法刷新：roomCode 为空')
    return
  }
  try {
    const res = await roomApi.getRoom(code)
    if (res.success && res.data) {
      const data = res.data as any
      if (data.players) {
        roomStore.setPlayers(data.players.map((p: any) => ({
          id: p.id || 0,
          userId: p.userId,
          nickname: p.nickname,
          seatIndex: p.seatIndex,
          chips: p.chips,
          isReady: p.isReady,
          isOnline: p.isOnline,
          isOwner: p.isOwner || data.ownerId === p.userId
        })))
      }
    }
  } catch (e) {
    console.error('刷新房间信息失败', e)
  }
}

// 页面加载时获取 URL 参数
onLoad(async (options) => {
  console.log('[Room] onLoad options:', options)
  const roomCode = options?.roomCode || uni.getStorageSync('currentRoomCode')
  console.log('[Room] roomCode:', roomCode)

  if (!roomCode) {
    uni.showToast({ title: '房间号不存在', icon: 'none' })
    setTimeout(() => uni.navigateBack(), 1500)
    return
  }

  // 保存到本地变量和 storage
  currentRoomCode.value = roomCode
  uni.setStorageSync('currentRoomCode', roomCode)

  try {
    // 先调用 joinRoom API 加入房间（如果已在房间中会自动处理）
    try {
      const joinRes = await roomApi.joinRoom(roomCode)
      console.log('[Room] joinRoom response:', joinRes)
      if (!joinRes.success) {
        uni.showToast({ title: joinRes.message || '加入房间失败', icon: 'none' })
        setTimeout(() => uni.navigateBack(), 1500)
        return
      }
    } catch (joinErr: any) {
      console.error('[Room] joinRoom error:', joinErr)
      uni.showToast({ title: joinErr.message || '加入房间失败', icon: 'none' })
      setTimeout(() => uni.navigateBack(), 1500)
      return
    }

    // 然后获取房间信息
    const res = await roomApi.getRoom(roomCode)
    console.log('[Room] getRoom response:', res)

    if (res.success && res.data) {
      const data = res.data as any
      roomStore.setRoom({
        roomCode: data.roomCode,
        roomId: data.id || data.roomId || 0,
        ownerId: data.ownerId,
        maxPlayers: data.maxPlayers,
        smallBlind: data.smallBlind,
        bigBlind: data.bigBlind,
        status: data.status
      })
      // 设置玩家列表
      if (data.players) {
        roomStore.setPlayers(data.players.map((p: any) => ({
          id: p.id || 0,
          userId: p.userId,
          nickname: p.nickname,
          seatIndex: p.seatIndex,
          chips: p.chips,
          isReady: p.isReady,
          isOnline: p.isOnline,
          isOwner: p.isOwner || data.ownerId === p.userId
        })))
      }
      await connect()
      await joinSignalR(roomCode)

      // 设置页面级别的事件监听（全局事件已在 useSignalR 中设置）
      setupPageEventListeners()
    } else {
      uni.showToast({ title: res.message || '获取房间信息失败', icon: 'none' })
      setTimeout(() => uni.navigateBack(), 1500)
    }
  } catch (e: any) {
    console.error('[Room] getRoom error:', e)
    uni.showToast({ title: e.message || '获取房间信息失败', icon: 'none' })
    setTimeout(() => uni.navigateBack(), 1500)
  }
})

// 是否正在跳转到游戏页面（此时不应该离开房间）
const isNavigatingToGame = ref(false)

// 页面卸载时（返回键等）离开房间
onUnload(async () => {
  console.log('[Room] onUnload - isNavigatingToGame:', isNavigatingToGame.value)

  // 如果是跳转到游戏页面，不离开房间
  if (isNavigatingToGame.value) {
    return
  }

  try {
    const code = currentRoomCode.value || roomStore.roomCode
    if (code) {
      await leaveSignalR(code)
    }
  } catch (e) {
    // ignore
  }
  roomStore.clear()
})

onUnmounted(() => {
  // 移除页面级别的事件监听
  cleanupPageEventListeners()
})
</script>

<style scoped>
.container {
  min-height: 100vh;
  padding: 40rpx;
  box-sizing: border-box;
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
  justify-content: center;
  margin: 0 -10rpx 40rpx -10rpx;
}

.seat {
  width: 30%;
  height: 160rpx;
  margin: 0 1.66% 20rpx 1.66%;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 16rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  position: relative;
  box-sizing: border-box;
}

.seat.occupied {
  background: rgba(233, 69, 96, 0.3);
}

.seat.clickable {
  background: rgba(255, 255, 255, 0.15);
  border: 2rpx dashed rgba(255, 255, 255, 0.3);
}

.seat.clickable:active {
  background: rgba(255, 255, 255, 0.25);
}

.seat.current {
  border: 3rpx solid #ffd700;
}

.owner-badge {
  position: absolute;
  top: 6rpx;
  left: 6rpx;
  padding: 4rpx 12rpx;
  background: linear-gradient(135deg, #ffd700, #ffb700);
  border-radius: 8rpx;
}

.owner-badge text {
  font-size: 18rpx;
  color: #333333;
  font-weight: bold;
}

.player-name {
  font-size: 24rpx;
  color: #ffffff;
  font-weight: bold;
  max-width: 90%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.player-chips {
  font-size: 20rpx;
  color: #ffd700;
  margin-top: 6rpx;
}

.ready-badge {
  position: absolute;
  bottom: 6rpx;
  right: 6rpx;
  padding: 4rpx 12rpx;
  background: #4a69bd;
  border-radius: 8rpx;
}

.ready-badge text {
  font-size: 18rpx;
  color: #ffffff;
}

.empty-seat {
  font-size: 24rpx;
  color: #999999;
}

.tip-text {
  text-align: center;
  padding: 30rpx 0;
}

.tip-text text {
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

.btn-success {
  background: linear-gradient(135deg, #27ae60, #2ecc71);
  color: #ffffff;
}

.btn-outline {
  background: transparent;
  color: #ffffff;
  border: 2rpx solid #ffffff;
}
</style>
