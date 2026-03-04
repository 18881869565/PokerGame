<template>
  <view class="action-bar">
    <!-- 看牌按钮（随时可见，只要还没看过牌且还在游戏中） -->
    <view class="look-btn-area" v-if="!hasLooked && isInGame">
      <view class="action-btn look-btn" @click="$emit('look')">
        <text class="btn-text">看牌</text>
      </view>
    </view>

    <!-- 未到我的回合 -->
    <template v-if="!isMyTurn">
      <view class="waiting-text" v-if="hasLooked || !isInGame">等待其他玩家操作...</view>
    </template>

    <!-- 我的回合 - 操作按钮 -->
    <template v-else>
      <view class="action-row top-row">
        <!-- 下注按钮 -->
        <view
          v-if="availableActions.includes(2) || availableActions.includes(3)"
          class="action-btn bet-btn"
          @click="$emit('bet')"
        >
          <text class="btn-text">{{ hasLooked ? '跟注' : '闷牌' }}</text>
          <text class="btn-amount">{{ minBetAmount }}</text>
        </view>

        <!-- 弃牌按钮 -->
        <view
          v-if="availableActions.includes(5)"
          class="action-btn fold-btn"
          @click="$emit('fold')"
        >
          <text class="btn-text">弃牌</text>
        </view>
      </view>

      <!-- 第二排按钮 -->
      <view class="action-row bottom-row">
        <!-- 加注按钮 -->
        <view
          v-if="availableActions.includes(2) || availableActions.includes(3)"
          class="action-btn raise-btn"
          @click="showRaisePicker"
        >
          <text class="btn-text">加注</text>
        </view>

        <!-- 比牌按钮 -->
        <view
          v-if="availableActions.includes(4)"
          class="action-btn compare-btn"
          @click="showComparePicker"
        >
          <text class="btn-text">比牌</text>
          <text class="btn-amount">{{ compareCost }}</text>
        </view>
      </view>
    </template>

    <!-- 加注选择弹窗 -->
    <view v-if="showRaiseModal" class="picker-modal" @click="showRaiseModal = false">
      <view class="picker-content" @click.stop>
        <text class="picker-title">选择加注金额</text>
        <scroll-view class="raise-options" scroll-y>
          <view
            v-for="amount in raiseOptions"
            :key="amount"
            class="raise-option"
            @click="selectRaise(amount)"
          >
            <text>{{ amount }}</text>
          </view>
        </scroll-view>
      </view>
    </view>

    <!-- 比牌选择弹窗 -->
    <view v-if="showCompareModal" class="picker-modal" @click="showCompareModal = false">
      <view class="picker-content" @click.stop>
        <text class="picker-title">选择比牌对象</text>
        <scroll-view class="compare-options" scroll-y>
          <view
            v-for="player in compareablePlayers"
            :key="player.userId"
            class="compare-option"
            @click="selectCompare(player.userId)"
          >
            <text class="player-name">{{ player.nickname }}</text>
            <text class="player-chips">筹码: {{ player.chips }}</text>
          </view>
        </scroll-view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { ZjhPlayer } from '@/stores/zjhGame'

const props = defineProps<{
  isMyTurn: boolean
  hasLooked: boolean
  isInGame: boolean
  minBetAmount: number
  compareCost: number
  myChips: number
  betAmount: number
  availableActions: number[]
  players: ZjhPlayer[]
  myUserId: number
}>()

const emit = defineEmits<{
  (e: 'look'): void
  (e: 'bet'): void
  (e: 'raise', amount: number): void
  (e: 'compare', targetUserId: number): void
  (e: 'fold'): void
}>()

const showRaiseModal = ref(false)
const showCompareModal = ref(false)

const raiseOptions = computed(() => {
  const options: number[] = []
  const base = props.betAmount
  // 生成加注选项：2倍、4倍、8倍
  for (let i = 2; i <= 8; i *= 2) {
    const amount = base * i
    if (amount <= props.myChips && amount > base) {
      options.push(amount)
    }
  }
  return options
})

// 可比牌的玩家列表
const compareablePlayers = computed(() => {
  return props.players.filter(p =>
    p.userId !== props.myUserId && p.isInGame
  )
})

// 显示加注选择
const showRaisePicker = () => {
  if (raiseOptions.value.length > 0) {
    showRaiseModal.value = true
  } else {
    uni.showToast({ title: '无法加注', icon: 'none' })
  }
}

// 选择加注金额
const selectRaise = (amount: number) => {
  showRaiseModal.value = false
  emit('raise', amount)
}

// 显示比牌选择
const showComparePicker = () => {
  if (compareablePlayers.value.length === 0) {
    uni.showToast({ title: '没有可比牌的对象', icon: 'none' })
    return
  }
  if (compareablePlayers.value.length === 1) {
    // 只有一个对象，直接比牌
    emit('compare', compareablePlayers.value[0].userId)
  } else {
    showCompareModal.value = true
  }
}

// 选择比牌对象
const selectCompare = (targetUserId: number) => {
  showCompareModal.value = false
  emit('compare', targetUserId)
}
</script>

<style scoped>
.action-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: linear-gradient(180deg, rgba(0, 0, 0, 0.8) 0%, rgba(0, 0, 0, 0.95) 100%);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 0 20rpx;
  z-index: 100;
}

.look-btn-area {
  padding: 10rpx 0;
}

.waiting-text {
  color: rgba(255, 255, 255, 0.6);
  font-size: 28rpx;
  padding: 30rpx 0;
}

/* 按钮行布局 */
.action-row.top-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16rpx;
  padding: 10rpx 0;
}

.action-row.bottom-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16rpx;
  padding: 10rpx 0;
}

.action-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-width: 140rpx;
  height: 90rpx;
  border-radius: 16rpx;
  padding: 0 20rpx;
}

.btn-text {
  color: #ffffff;
  font-size: 26rpx;
  font-weight: bold;
}

.btn-amount {
  color: rgba(255, 255, 255, 0.8);
  font-size: 20rpx;
  margin-top: 4rpx;
}

.look-btn {
  background: linear-gradient(135deg, #3498db 0%, #2980b9 100%);
}

.bet-btn {
  background: linear-gradient(135deg, #2ecc71 0%, #27ae60 100%);
}

.raise-btn {
  background: linear-gradient(135deg, #f39c12 0%, #e67e22 100%);
}

.compare-btn {
  background: linear-gradient(135deg, #9b59b6 0%, #8e44ad 100%);
}

.fold-btn {
  background: linear-gradient(135deg, #95a5a6 0%, #7f8c8d 100%);
}

/* 弹窗样式 */
.picker-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 300;
}

.picker-content {
  width: 600rpx;
  max-height: 600rpx;
  background: #2c3e50;
  border-radius: 20rpx;
  padding: 30rpx;
}

.picker-title {
  display: block;
  text-align: center;
  color: #ffffff;
  font-size: 32rpx;
  font-weight: bold;
  margin-bottom: 30rpx;
}

.raise-options, .compare-options {
  max-height: 400rpx;
}

.raise-option, .compare-option {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24rpx;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 12rpx;
  margin-bottom: 16rpx;
}

.raise-option:active, .compare-option:active {
  background: rgba(255, 255, 255, 0.2);
}

.raise-option text {
  color: #ffffff;
  font-size: 32rpx;
}

.compare-option {
  flex-direction: column;
  gap: 8rpx;
}

.player-name {
  color: #ffffff;
  font-size: 28rpx;
  font-weight: bold;
}

.player-chips {
  color: rgba(255, 255, 255, 0.6);
  font-size: 22rpx;
}
</style>
