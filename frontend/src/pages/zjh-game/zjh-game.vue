<template>
  <view class="game-page">
    <!-- 顶部导航栏 -->
    <view class="top-bar">
      <view class="back-btn" @click="handleBack">
        <Icon name="back" :size="36" color="#ffffff" />
      </view>
      <text class="phase-text">{{ zjhGameStore.phaseText }}</text>
      <view class="info-area">
        <text class="pot-text">底池: {{ formatChips(zjhGameStore.pot) }}</text>
      </view>
    </view>

    <!-- 牌桌区域 -->
    <view class="table-area">
      <!-- 椭圆形牌桌 -->
      <view class="poker-table">
        <!-- 底池显示 -->
        <view class="pot-area">
          <PotDisplay :pot="zjhGameStore.pot" />
        </view>

        <!-- 当前下注信息 -->
        <view class="bet-info">
          <text class="bet-label">当前下注: {{ zjhGameStore.betAmount }}</text>
          <text class="round-label">第 {{ zjhGameStore.roundCount }}/{{ zjhGameStore.maxRounds }} 轮</text>
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
      <text class="my-cards-label">
        {{ zjhGameStore.hasLooked ? '我的手牌' : '手牌（未看牌）' }}
      </text>
      <view class="my-cards">
        <!-- 已看牌或游戏结束时显示 -->
        <template v-if="zjhGameStore.hasLooked || zjhGameStore.phase === 4">
          <PokerCard
            v-for="(card, index) in zjhGameStore.myHand"
            :key="'h' + index"
            :suit="card.suit"
            :rank="card.rank"
            size="large"
          />
        </template>
        <!-- 未看牌时显示背面 -->
        <template v-else>
          <view v-for="i in 3" :key="'back' + i" class="card-back">
            <PokerCard :suit="0" :rank="2" :face-down="true" size="large" />
          </view>
        </template>
      </view>
    </view>

    <!-- 操作按钮栏 -->
    <ZjhActionBar
      :is-my-turn="zjhGameStore.isMyTurn"
      :has-looked="zjhGameStore.hasLooked"
      :is-in-game="isInGame"
      :min-bet-amount="zjhGameStore.myBetCost"
      :compare-cost="zjhGameStore.myCompareCost"
      :my-chips="zjhGameStore.myChips"
      :bet-amount="zjhGameStore.betAmount"
      :available-actions="availableActionsList"
      :players="zjhGameStore.players"
      :my-user-id="zjhGameStore.myUserId"
      @look="onLook"
      @bet="onBet"
      @raise="onRaise"
      @compare="onCompare"
      @fold="onFold"
    />

    <!-- 游戏结束弹窗 -->
    <view v-if="showResultModal" class="result-modal">
      <view class="result-content">
        <text class="result-title">🎉 游戏结束</text>

        <!-- 获胜者 -->
        <view class="winners">
          <text class="winners-label">获胜者:</text>
          <text class="winner-name">{{ winnerNickname }}</text>
        </view>

        <!-- 底池 -->
        <text class="result-pot">赢得底池: {{ formatChips(zjhGameStore.gameResult?.pot || 0) }}</text>

        <!-- 获胜者牌型 -->
        <text v-if="zjhGameStore.gameResult?.handDescription" class="hand-type">
          牌型: {{ zjhGameStore.gameResult.handDescription }}
        </text>

        <!-- 所有玩家手牌 -->
        <view v-if="zjhGameStore.gameResult?.playerHands?.length" class="all-hands">
          <text class="all-hands-title">所有玩家手牌</text>
          <view
            v-for="hand in zjhGameStore.gameResult.playerHands"
            :key="hand.userId"
            class="player-hand"
            :class="{ winner: hand.isWinner }"
          >
            <text class="hand-player-name">{{ hand.nickname }}</text>
            <view class="hand-cards">
              <PokerCard
                v-for="(card, idx) in hand.hand"
                :key="idx"
                :suit="card.suit"
                :rank="card.rank"
                size="small"
              />
            </view>
            <text v-if="hand.handDescription" class="hand-desc">{{ hand.handDescription }}</text>
            <text v-if="hand.isWinner" class="winner-badge">胜</text>
          </view>
        </view>

        <button class="close-btn" @click="closeResultModal">继续游戏</button>
      </view>
    </view>

    <!-- 比牌输家看牌弹窗 -->
    <view v-if="zjhGameStore.compareLoseResult" class="compare-lose-modal">
      <view class="compare-lose-content">
        <text class="lose-title">😔 比牌失败</text>
        <text class="lose-desc">你的牌型是：{{ zjhGameStore.compareLoseResult.handDescription }}</text>
        <view class="lose-cards">
          <PokerCard
            v-for="(card, idx) in zjhGameStore.compareLoseResult.hand"
            :key="idx"
            :suit="card.suit"
            :rank="card.rank"
            size="large"
          />
        </view>
        <button class="confirm-btn" @click="closeCompareLoseModal">知道了</button>
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
import { useZjhGameStore, ZjhGamePhase, type ZjhPlayer } from '@/stores/zjhGame'
import { useUserStore } from '@/stores/user'
import { useRoomStore } from '@/stores/room'
import { useSignalR } from '@/composables/useSignalR'
import PokerCard from '@/components/PokerCard.vue'
import PlayerSeat from '@/components/PlayerSeat.vue'
import ZjhActionBar from '@/components/ZjhActionBar.vue'
import PotDisplay from '@/components/PotDisplay.vue'
import Icon from '@/components/Icon.vue'

const zjhGameStore = useZjhGameStore()
const userStore = useUserStore()
const roomStore = useRoomStore()
const {
  isConnected,
  connect,
  joinRoom,
  leaveRoom,
  zjhLook,
  zjhBet,
  zjhRaise,
  zjhCompare,
  zjhFold
} = useSignalR()

const loading = ref(true)
const roomCode = ref('')

// 可用操作列表
const availableActionsList = computed(() => {
  return zjhGameStore.availableActions?.actions || []
})

// 当前用户是否还在游戏中
const isInGame = computed(() => {
  const me = zjhGameStore.currentUser
  return me?.isInGame ?? false
})

// 显示结果弹窗
const showResultModal = computed(() =>
  zjhGameStore.phase === ZjhGamePhase.Finished && zjhGameStore.gameResult !== null
)

// 获胜者昵称
const winnerNickname = computed(() => {
  return zjhGameStore.gameResult?.winnerNickname || '未知'
})

// 获取指定座位的玩家
const getPlayerAtSeat = (seatIndex: number): ZjhPlayer | undefined => {
  return zjhGameStore.players.find(p => p.seatIndex === seatIndex)
}

// 判断玩家是否是当前操作者
const isPlayerActive = (seatIndex: number): boolean => {
  const player = getPlayerAtSeat(seatIndex)
  return player?.userId === zjhGameStore.currentPlayerId
}

// 格式化筹码
const formatChips = (chips: number): string => {
  if (chips >= 10000) {
    return (chips / 10000).toFixed(1) + '万'
  }
  return chips.toString()
}

// 操作处理
const onLook = () => {
  if (roomCode.value) zjhLook(roomCode.value)
}

const onBet = () => {
  if (roomCode.value) zjhBet(roomCode.value)
}

const onRaise = (amount: number) => {
  if (roomCode.value) zjhRaise(roomCode.value, amount)
}

const onCompare = (targetUserId: number) => {
  if (roomCode.value) zjhCompare(roomCode.value, targetUserId)
}

const onFold = () => {
  if (roomCode.value) zjhFold(roomCode.value)
}

// 处理返回按钮点击
const handleBack = () => {
  const gameInProgress = zjhGameStore.phase !== ZjhGamePhase.Waiting &&
                   zjhGameStore.phase !== ZjhGamePhase.Finished

  const message = gameInProgress
    ? '正在游戏中，离开将视为弃牌，确定要离开吗？'
    : '确定要离开房间返回大厅吗？'

  uni.showModal({
    title: '离开房间',
    content: message,
    confirmText: '确定离开',
    cancelText: '取消',
    success: async (res) => {
      if (res.confirm) {
        // 如果正在游戏中，先弃牌
        if (gameInProgress) {
          zjhFold(roomCode.value)
          await new Promise(resolve => setTimeout(resolve, 300))
        }

        // 离开房间
        if (roomCode.value) {
          await leaveRoom(roomCode.value)
        }

        // 重置状态
        zjhGameStore.reset()
        roomStore.clear()

        // 返回大厅
        isNavigatingBack.value = true
        uni.switchTab({ url: '/pages/lobby/lobby' })
      }
    }
  })
}

// 关闭结果弹窗
const closeResultModal = async () => {
  zjhGameStore.clearGameResult()
  await userStore.fetchUserInfo()

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

  uni.navigateBack()
}

// 关闭比牌输家弹窗
const closeCompareLoseModal = () => {
  zjhGameStore.setCompareLoseResult(null)
}

// 页面加载
onLoad(async (options) => {
  console.log('[ZjhGame] onLoad options:', options)
  roomCode.value = options?.roomCode || uni.getStorageSync('currentRoomCode') || ''
  console.log('[ZjhGame] roomCode:', roomCode.value)

  // 设置用户ID
  if (userStore.userInfo?.id) {
    zjhGameStore.setMyUserId(userStore.userInfo.id)
  }

  // 连接 SignalR 并加入房间
  if (roomCode.value) {
    zjhGameStore.setRoom(roomCode.value, 0)

    try {
      await connect()
      await joinRoom(roomCode.value)
      console.log('[ZjhGame] SignalR connected and joined room')
    } catch (e: any) {
      console.error('[ZjhGame] SignalR connection error:', e)
      uni.showToast({ title: e.message || '连接失败', icon: 'none' })
    }
  }

  loading.value = false
})

// 是否正在返回房间
const isNavigatingBack = ref(false)

// 页面卸载
onUnload(() => {
  if (isNavigatingBack.value) {
    return
  }
  zjhGameStore.reset()
})

onUnmounted(() => {
  zjhGameStore.reset()
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

.info-area {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
}

.pot-text {
  color: #f39c12;
  font-size: 24rpx;
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
  width: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  margin-bottom: 20rpx;
}

/* 下注信息 */
.bet-info {
  display: flex;
  gap: 20rpx;
}

.bet-label, .round-label {
  color: rgba(255, 255, 255, 0.8);
  font-size: 24rpx;
}

/* 我的手牌区域 */
.my-cards-area {
  position: fixed;
  bottom: 320rpx;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  flex-direction: column;
  align-items: center;
  z-index: 50;
}

.my-cards-label {
  color: rgba(255, 255, 255, 0.6);
  font-size: 24rpx;
  margin-bottom: 12rpx;
}

.my-cards {
  display: flex;
  gap: 16rpx;
}

.card-back {
  opacity: 0.9;
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
  margin-bottom: 20rpx;
}

.hand-type {
  display: block;
  text-align: center;
  font-size: 26rpx;
  color: #9b59b6;
  margin-bottom: 20rpx;
}

.all-hands {
  margin-bottom: 30rpx;
}

.all-hands-title {
  display: block;
  text-align: center;
  font-size: 26rpx;
  color: #666666;
  margin-bottom: 20rpx;
}

.player-hand {
  display: flex;
  align-items: center;
  padding: 16rpx 0;
  border-bottom: 1rpx solid #eeeeee;
  position: relative;
}

.player-hand.winner {
  background: rgba(46, 204, 113, 0.1);
  border-radius: 8rpx;
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

.winner-badge {
  background: #27ae60;
  color: #ffffff;
  font-size: 22rpx;
  padding: 4rpx 12rpx;
  border-radius: 8rpx;
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

/* 比牌输家弹窗 */
.compare-lose-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 250;
}

.compare-lose-content {
  width: 560rpx;
  background: linear-gradient(180deg, #2c3e50 0%, #1a252f 100%);
  border-radius: 24rpx;
  padding: 40rpx;
  text-align: center;
  border: 2rpx solid #e74c3c;
  box-shadow: 0 8rpx 32rpx rgba(231, 76, 60, 0.3);
}

.lose-title {
  display: block;
  font-size: 36rpx;
  font-weight: bold;
  color: #e74c3c;
  margin-bottom: 24rpx;
}

.lose-desc {
  display: block;
  font-size: 28rpx;
  color: #ecf0f1;
  margin-bottom: 30rpx;
}

.lose-cards {
  display: flex;
  justify-content: center;
  gap: 16rpx;
  margin-bottom: 30rpx;
}

.confirm-btn {
  width: 100%;
  padding: 20rpx 0;
  background: #e74c3c;
  color: #ffffff;
  font-size: 28rpx;
  font-weight: bold;
  border-radius: 12rpx;
  border: none;
}

.confirm-btn::after {
  border: none;
}
</style>
