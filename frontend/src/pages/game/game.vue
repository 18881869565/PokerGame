<template>
  <view class="game-page">
    <!-- 游戏阶段指示器 -->
    <view class="phase-indicator">
      <text class="phase-text">{{ gameStore.phaseText }}</text>
      <text class="pot-text">底池: {{ formatChips(gameStore.pot) }}</text>
    </view>

    <!-- 牌桌区域 -->
    <view class="table-area">
      <!-- 椭圆形牌桌 -->
      <view class="poker-table">
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
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { onLoad, onUnload } from '@dcloudio/uni-app'
import { useGameStore, GamePhase } from '@/stores/game'
import { useUserStore } from '@/stores/user'
import { useSignalR } from '@/composables/useSignalR'
import PokerCard from '@/components/PokerCard.vue'
import PlayerSeat from '@/components/PlayerSeat.vue'
import ActionBar from '@/components/ActionBar.vue'

const gameStore = useGameStore()
const userStore = useUserStore()
const {
  isConnected,
  connect,
  disconnect,
  joinRoom,
  leaveRoom,
  fold,
  check,
  bet,
  raise,
  allIn
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

// 关闭结果弹窗
const closeResultModal = () => {
  gameStore.clearGameResult()
}

// 页面加载
onLoad(async (options) => {
  roomCode.value = options.roomCode || ''

  // 设置用户ID
  if (userStore.userInfo?.id) {
    gameStore.setMyUserId(userStore.userInfo.id)
  }

  // 连接 SignalR 并加入房间
  if (roomCode.value) {
    gameStore.setRoom(roomCode.value, 0)
    await connect()
    await joinRoom(roomCode.value)
  }

  loading.value = false
})

// 页面卸载
onUnload(() => {
  if (roomCode.value) {
    leaveRoom(roomCode.value)
  }
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

.pot-text {
  color: #ffffff;
  font-size: 26rpx;
}

/* 牌桌区域 */
.table-area {
  position: relative;
  margin-top: 100rpx;
  height: 750rpx;
}

/* 椭圆形牌桌 */
.poker-table {
  position: absolute;
  top: 50rpx;
  left: 50%;
  transform: translateX(-50%);
  width: 650rpx;
  height: 400rpx;
  background: linear-gradient(135deg, #2d5016 0%, #1a6b3c 50%, #2d5016 100%);
  border-radius: 200rpx;
  border: 16rpx solid #5c3d2e;
  box-shadow:
    inset 0 0 30rpx rgba(0, 0, 0, 0.3),
    0 10rpx 30rpx rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
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
