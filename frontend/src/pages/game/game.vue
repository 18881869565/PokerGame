<template>
  <view class="game-container">
    <!-- 游戏桌面 -->
    <view class="poker-table">
      <!-- 公共牌区域 -->
      <view class="community-cards">
        <view class="card" v-for="(card, index) in gameStore.communityCards" :key="index">
          <text class="card-value">{{ getCardDisplay(card) }}</text>
        </view>
        <view class="card placeholder" v-for="i in (5 - gameStore.communityCards.length)" :key="'p' + i">
          <text class="card-value">?</text>
        </view>
      </view>

      <!-- 底池 -->
      <view class="pot">
        <text>底池: {{ gameStore.pot }}</text>
      </view>
    </view>

    <!-- 玩家座位 (简化版，围绕桌子排列) -->
    <view class="players-area">
      <view
        class="player-seat"
        v-for="player in gameStore.players"
        :key="player.userId"
        :class="{ active: player.userId === gameStore.currentPlayerId, folded: player.status === 2 }"
      >
        <text class="player-name">{{ player.seatIndex }}号位</text>
        <text class="player-chips">{{ player.chips }}</text>
        <text class="player-bet" v-if="player.currentBet > 0">下注: {{ player.currentBet }}</text>
      </view>
    </view>

    <!-- 我的手牌 -->
    <view class="my-cards">
      <text class="label">我的手牌</text>
      <view class="cards-row">
        <view class="card my-card" v-for="(card, index) in gameStore.myCards" :key="index">
          <text class="card-value">{{ getCardDisplay(card) }}</text>
        </view>
      </view>
    </view>

    <!-- 操作按钮 -->
    <view class="action-bar" v-if="isMyTurn">
      <button class="action-btn fold" @click="doFold">弃牌</button>
      <button class="action-btn check" @click="doCheck" v-if="canCheck">过牌</button>
      <button class="action-btn call" @click="doCall">跟注</button>
      <button class="action-btn raise" @click="showRaiseSlider = true">加注</button>
      <button class="action-btn allin" @click="doAllIn">全押</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useGameStore } from '@/stores/game'
import { useUserStore } from '@/stores/user'
import { useSignalR } from '@/composables/useSignalR'

const gameStore = useGameStore()
const userStore = useUserStore()
const { fold, check, bet, allIn } = useSignalR()

const isMyTurn = computed(() => gameStore.currentPlayerId === userStore.userInfo?.id)
const canCheck = computed(() => true) // TODO: 根据当前下注情况判断
const showRaiseSlider = ref(false)

const getCardDisplay = (card: { suit: number; rank: number }) => {
  const suits = ['♠', '♥', '♣', '♦']
  const ranks = ['', '', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K', 'A']
  return `${suits[card.suit]}${ranks[card.rank]}`
}

const doFold = async () => {
  await fold(gameStore.roomCode || '')
}

const doCheck = async () => {
  await check(gameStore.roomCode || '')
}

const doCall = async () => {
  // TODO: 计算跟注金额
  await bet(gameStore.roomCode || '', 0)
}

const doAllIn = async () => {
  await allIn(gameStore.roomCode || '')
}
</script>

<style scoped>
.game-container {
  min-height: 100vh;
  background: #0f3460;
  position: relative;
}

.poker-table {
  width: 90%;
  margin: 40rpx auto;
  aspect-ratio: 2;
  background: #1a6b3c;
  border-radius: 50%;
  border: 10rpx solid #8b4513;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.community-cards {
  display: flex;
  gap: 10rpx;
}

.card {
  width: 60rpx;
  height: 84rpx;
  background: #ffffff;
  border-radius: 8rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.card.placeholder {
  background: rgba(255, 255, 255, 0.3);
}

.card-value {
  font-size: 24rpx;
  color: #333333;
  font-weight: bold;
}

.pot {
  margin-top: 20rpx;
  color: #ffd700;
  font-size: 28rpx;
  font-weight: bold;
}

.players-area {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 100%;
  height: 100%;
}

.player-seat {
  position: absolute;
  background: rgba(0, 0, 0, 0.5);
  padding: 10rpx 20rpx;
  border-radius: 10rpx;
  text-align: center;
}

.player-seat.active {
  border: 2rpx solid #ffd700;
}

.player-seat.folded {
  opacity: 0.5;
}

.player-name {
  font-size: 24rpx;
  color: #ffffff;
  display: block;
}

.player-chips {
  font-size: 20rpx;
  color: #ffd700;
}

.player-bet {
  font-size: 18rpx;
  color: #e94560;
}

.my-cards {
  position: fixed;
  bottom: 200rpx;
  left: 50%;
  transform: translateX(-50%);
  text-align: center;
}

.label {
  font-size: 24rpx;
  color: #999999;
  display: block;
  margin-bottom: 10rpx;
}

.cards-row {
  display: flex;
  gap: 20rpx;
}

.my-card {
  width: 80rpx;
  height: 112rpx;
}

.my-card .card-value {
  font-size: 32rpx;
}

.action-bar {
  position: fixed;
  bottom: 40rpx;
  left: 0;
  right: 0;
  display: flex;
  justify-content: center;
  gap: 20rpx;
  padding: 0 40rpx;
}

.action-btn {
  padding: 20rpx 30rpx;
  border-radius: 10rpx;
  font-size: 28rpx;
  font-weight: bold;
  color: #ffffff;
  border: none;
}

.action-btn.fold { background: #666666; }
.action-btn.check { background: #4a69bd; }
.action-btn.call { background: #27ae60; }
.action-btn.raise { background: #f39c12; }
.action-btn.allin { background: #e94560; }
</style>
