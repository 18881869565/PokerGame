<template>
  <view class="game-page">
    <!-- 顶部导航栏 -->
    <view class="top-bar">
      <view class="back-btn" @click="handleBack">
        <Icon name="back" :size="36" color="#ffffff" />
      </view>
      <text class="phase-text">{{ gameStore.phaseText }}</text>
      <view class="placeholder"></view>
    </view>

    <!-- 牌桌区域 -->
    <view class="table-area">
      <!-- 椭圆形牌桌 -->
      <view class="poker-table">
        <!-- 底池显示 -->
        <view class="pot-area">
          <PotDisplay :pot="gameStore.pot" />
        </view>

        <!-- 公共牌 -->
        <view class="community-cards">
          <PokerCard
            v-for="(card, index) in gameStore.communityCards"
            :key="'c' + index"
            :suit="card.suit"
            :rank="card.rank"
            size="medium"
          />
          <!-- 占位牌 -->
          <view
            v-for="i in (5 - gameStore.communityCards.length)"
            :key="'p' + i"
            class="card-placeholder"
          >
            <PokerCard :suit="0" :rank="2" :face-down="true" size="medium" />
          </view>
        </view>
      </view>

      <!-- 玩家座位（9人固定布局） -->
      <PlayerSeat
        v-for="seatIndex in 9"
        :key="'seat' + seatIndex"
        :seat-index="seatIndex - 1"
        :player="getPlayerAtSeat(seatIndex - 1)"
        :is-active="isPlayerActive(seatIndex - 1)"
      />
    </view>

    <!-- 我的手牌区域 -->
    <view class="my-cards-area">
      <text class="my-cards-label">我的手牌</text>
      <view class="my-cards">
        <PokerCard
          v-for="(card, index) in myHoleCards"
          :key="'h' + index"
          :suit="card.suit"
          :rank="card.rank"
          size="large"
        />
        <!-- 没有牌时显示占位 -->
        <template v-if="myHoleCards.length === 0">
          <view class="card-placeholder-large">
            <text>?</text>
          </view>
          <view class="card-placeholder-large">
            <text>?</text>
          </view>
        </template>
      </view>
    </view>

    <!-- 操作按钮栏 -->
    <ActionBar
      :is-my-turn="gameStore.isMyTurn"
      :can-check="gameStore.canCheck"
      :current-highest-bet="gameStore.currentHighestBet"
      :my-current-bet="gameStore.myCurrentBet"
      :my-chips="gameStore.myChips"
      :big-blind="gameStore.bigBlind"
      :pot="gameStore.pot"
      :has-all-in-player="gameStore.hasAllInPlayer"
      @fold="onFold"
      @check="onCheck"
      @call="onCall"
      @raise="onRaise"
      @all-in="onAllIn"
    />

    <!-- 游戏结束弹窗 -->
    <view v-if="showResultModal" class="result-modal">
      <view class="result-content">
        <text class="result-title">🎉 游戏结束</text>

        <!-- 获胜者 -->
        <view class="winners">
          <text class="winners-label">获胜者:</text>
          <text v-for="winnerId in gameStore.gameResult?.winnerIds" :key="winnerId" class="winner-name">
            {{ getPlayerName(winnerId) }}
          </text>
        </view>

        <!-- 底池 -->
        <text class="result-pot">赢得底池: {{ formatChips(gameStore.gameResult?.pot || 0) }}</text>

        <!-- 所有玩家手牌 -->
        <view class="all-hands">
          <view
            v-for="hand in gameStore.gameResult?.playerHands"
            :key="hand.userId"
            class="player-hand"
          >
            <text class="hand-player-name">{{ hand.nickname }}</text>
            <view class="hand-cards">
              <PokerCard
                v-for="(card, idx) in hand.holeCards"
                :key="idx"
                :suit="card.suit"
                :rank="card.rank"
                size="small"
              />
            </view>
            <text v-if="hand.handDescription" class="hand-desc">{{ hand.handDescription }}</text>
            <text v-if="hand.chipsWon > 0" class="chips-won">+{{ hand.chipsWon }}</text>
          </view>
        </view>

        <button class="close-btn" @click="closeResultModal">继续游戏</button>
      </view>
    </view>

    <!-- 加载中 -->
    <view v-if="loading" class="loading-mask">
      <text class="loading-text">加载中...</text>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted } from 'vue'
import { onLoad, onUnload } from '@dcloudio/uni-app'
import { useGameStore, GamePhase } from '@/stores/game'
import { useUserStore } from '@/stores/user'
import { useRoomStore } from '@/stores/room'
import { useSignalR } from '@/composables/useSignalR'
import PokerCard from '@/components/PokerCard.vue'
import PlayerSeat from '@/components/PlayerSeat.vue'
import ActionBar from '@/components/ActionBar.vue'
import PotDisplay from '@/components/PotDisplay.vue'
import Icon from '@/components/Icon.vue'

const gameStore = useGameStore()
const userStore = useUserStore()
const roomStore = useRoomStore()
const {
  isConnected,
  connect,
  joinRoom,
  leaveRoom,
  fold,
  check,
  bet,
  raise,
  allIn,
  connection
} = useSignalR()

const loading = ref(true)
const roomCode = ref('')

// 我的手牌
const myHoleCards = computed(() => {
  const me = gameStore.players.find(p => p.userId === gameStore.myUserId)
  return me?.holeCards || []
})

// 显示结果弹窗
const showResultModal = computed(() =>
  gameStore.phase === GamePhase.Finished && gameStore.gameResult !== null
)

// 获取指定座位的玩家
const getPlayerAtSeat = (seatIndex: number) => {
  return gameStore.players.find(p => p.seatIndex === seatIndex)
}

// 判断玩家是否是当前操作者
const isPlayerActive = (seatIndex: number) => {
  const player = getPlayerAtSeat(seatIndex)
  return player?.userId === gameStore.currentPlayerId
}

// 获取玩家名称
const getPlayerName = (userId: number) => {
  const player = gameStore.players.find(p => p.userId === userId)
  return player?.nickname || '玩家'
}

// 格式化筹码
const formatChips = (chips: number): string => {
  if (chips >= 10000) {
    return (chips / 10000).toFixed(1) + '万'
  }
  return chips.toString()
}

// 操作处理
const onFold = () => {
  if (roomCode.value) fold(roomCode.value)
}

const onCheck = () => {
  if (roomCode.value) check(roomCode.value)
}

const onCall = () => {
  if (roomCode.value) bet(roomCode.value, gameStore.callAmount)
}

const onRaise = (amount: number) => {
  if (roomCode.value) raise(roomCode.value, amount)
}

const onAllIn = () => {
  if (roomCode.value) allIn(roomCode.value)
}

// 处理返回按钮点击
const handleBack = () => {
  const isInGame = gameStore.phase !== GamePhase.Waiting &&
                   gameStore.phase !== GamePhase.Finished

  const message = isInGame
    ? '正在游戏中，离开将视为弃牌，确定要离开吗？'
    : '确定要离开游戏返回房间吗？'

  uni.showModal({
    title: '离开游戏',
    content: message,
    confirmText: '确定离开',
    cancelText: '取消',
    success: async (res) => {
      if (res.confirm) {
        // 如果正在游戏中，先弃牌
        if (isInGame && gameStore.isMyTurn) {
          onFold()
        } else if (isInGame && roomCode.value) {
          // 即使不是自己回合也要弃牌
          fold(roomCode.value)
        }

        // 等待一下让弃牌操作完成
        await new Promise(resolve => setTimeout(resolve, 300))

        // 标记正在返回，避免 onUnload 时的额外处理
        isNavigatingBack.value = true

        // 返回房间页面
        uni.navigateBack()
      }
    }
  })
}

// 关闭结果弹窗，返回房间准备下一局
const closeResultModal = async () => {
  gameStore.clearGameResult()
  // 更新用户信息（刷新服务器余额）
  await userStore.fetchUserInfo()

  // 检查是否被踢出房间（筹码不足）
  if (roomStore.wasRemoved) {
    const reason = roomStore.removeReason || '筹码不足，已自动离开房间'
    roomStore.clear()
    uni.showModal({
      title: '已离开房间',
      content: reason,
      showCancel: false,
      success: () => {
        uni.switchTab({ url: '/pages/lobby/lobby' })
      }
    })
    return
  }

  // 返回房间页面
  uni.navigateBack()
}

// 页面加载
onLoad(async (options) => {
  console.log('[Game] onLoad options:', options)
  roomCode.value = options?.roomCode || uni.getStorageSync('currentRoomCode') || ''
  console.log('[Game] roomCode:', roomCode.value)

  // 设置用户ID
  if (userStore.userInfo?.id) {
    gameStore.setMyUserId(userStore.userInfo.id)
  }

  // 连接 SignalR 并加入房间
  if (roomCode.value) {
    gameStore.setRoom(roomCode.value, 0)

    try {
      // 连接 SignalR（如果是已连接状态会复用现有连接）
      await connect()
      // 加入房间获取最新游戏状态
      await joinRoom(roomCode.value)
      console.log('[Game] SignalR connected and joined room')
    } catch (e: any) {
      console.error('[Game] SignalR connection error:', e)
      uni.showToast({ title: e.message || '连接失败', icon: 'none' })
    }
  }

  loading.value = false
})

// 是否正在返回房间（此时不应该离开房间）
const isNavigatingBack = ref(false)

// 页面卸载 - 玩家可能返回房间，不离开房间
onUnload(() => {
  // 只有当明确要离开时才调用 leaveRoom
  // 正常情况下游戏结束后返回房间，不需要离开
  if (isNavigatingBack.value) {
    // 返回房间，保持 SignalR 连接
    return
  }

  // 其他情况（如用户手动返回），也不离开房间
  // SignalR 连接保持（全局单例）
  gameStore.reset()
})

onUnmounted(() => {
  gameStore.reset()
})
</script>

<style scoped>
.game-page {
  min-height: 100vh;
  background: linear-gradient(180deg, #1a1a2e 0%, #16213e 100%);
  position: relative;
  overflow: hidden;
}

/* 顶部导航栏 */
.top-bar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 88rpx;
  padding: 0 30rpx;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: space-between;
  z-index: 100;
}

.back-btn {
  width: 64rpx;
  height: 64rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.1);
}

.back-btn:active {
  background: rgba(255, 255, 255, 0.2);
}

.phase-text {
  color: #ffd700;
  font-size: 28rpx;
  font-weight: bold;
}

.placeholder {
  width: 64rpx;
}

/* 阶段指示器 */
.phase-indicator {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  padding: 20rpx 30rpx;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: space-between;
  align-items: center;
  z-index: 50;
}

.phase-text {
  color: #ffd700;
  font-size: 28rpx;
  font-weight: bold;
}

/* 牌桌区域 */
.table-area {
  position: relative;
  margin-top: 80rpx;
  height: 620rpx;
}

/* 椭圆形牌桌 */
.poker-table {
  position: absolute;
  top: 40rpx;
  left: 50%;
  transform: translateX(-50%);
  width: 580rpx;
  height: 320rpx;
  background: linear-gradient(135deg, #2d5016 0%, #1a6b3c 50%, #2d5016 100%);
  border-radius: 160rpx;
  border: 12rpx solid #5c3d2e;
  box-shadow:
    inset 0 0 20rpx rgba(0, 0, 0, 0.3),
    0 8rpx 20rpx rgba(0, 0, 0, 0.5);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

/* 底池区域 */
.pot-area {
  margin-bottom: 20rpx;
}

/* 公共牌 */
.community-cards {
  display: flex;
  gap: 12rpx;
  padding: 20rpx;
}

.card-placeholder {
  opacity: 0.5;
}

/* 我的手牌区域 */
.my-cards-area {
  position: fixed;
  bottom: 180rpx;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  flex-direction: column;
  align-items: center;
  z-index: 60;
}

.my-cards-label {
  color: rgba(255, 255, 255, 0.6);
  font-size: 24rpx;
  margin-bottom: 12rpx;
}

.my-cards {
  display: flex;
  gap: 20rpx;
}

.card-placeholder-large {
  width: 100rpx;
  height: 140rpx;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 8rpx;
  border: 2rpx dashed rgba(255, 255, 255, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
}

.card-placeholder-large text {
  color: rgba(255, 255, 255, 0.3);
  font-size: 40rpx;
}

/* 游戏结果弹窗 */
.result-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;
}

.result-content {
  width: 600rpx;
  background: #ffffff;
  border-radius: 20rpx;
  padding: 40rpx;
  max-height: 80vh;
  overflow-y: auto;
}

.result-title {
  display: block;
  text-align: center;
  font-size: 36rpx;
  font-weight: bold;
  color: #333333;
  margin-bottom: 30rpx;
}

.winners {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16rpx;
  margin-bottom: 20rpx;
}

.winners-label {
  font-size: 28rpx;
  color: #666666;
}

.winner-name {
  font-size: 32rpx;
  font-weight: bold;
  color: #e74c3c;
}

.result-pot {
  display: block;
  text-align: center;
  font-size: 28rpx;
  color: #f39c12;
  margin-bottom: 30rpx;
}

.all-hands {
  margin-bottom: 30rpx;
}

.player-hand {
  display: flex;
  align-items: center;
  padding: 16rpx 0;
  border-bottom: 1rpx solid #eeeeee;
}

.hand-player-name {
  width: 120rpx;
  font-size: 24rpx;
  color: #333333;
}

.hand-cards {
  display: flex;
  gap: 8rpx;
  margin: 0 20rpx;
}

.hand-desc {
  flex: 1;
  font-size: 22rpx;
  color: #666666;
}

.chips-won {
  font-size: 26rpx;
  font-weight: bold;
  color: #27ae60;
}

.close-btn {
  width: 100%;
  padding: 24rpx 0;
  background: #27ae60;
  color: #ffffff;
  font-size: 30rpx;
  font-weight: bold;
  border-radius: 12rpx;
  border: none;
}

.close-btn::after {
  border: none;
}

/* 加载中 */
.loading-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 300;
}

.loading-text {
  color: #ffffff;
  font-size: 32rpx;
}
</style>
