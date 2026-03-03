<template>
  <view
    class="player-seat"
    :class="seatClasses"
    :style="seatStyle"
  >
    <!-- 空座位 -->
    <template v-if="isEmpty">
      <view class="empty-seat">
        <text class="empty-text">空</text>
      </view>
    </template>

    <!-- 有玩家 -->
    <template v-else>
      <!-- 头像 -->
      <view class="avatar-wrapper">
        <image
          v-if="player.avatar"
          :src="player.avatar"
          class="avatar"
          mode="aspectFill"
        />
        <view v-else class="avatar avatar-default">
          <text class="avatar-text">{{ player.nickname?.charAt(0) || '?' }}</text>
        </view>
        <!-- 状态标签 -->
        <view v-if="isDealer" class="badge dealer">D</view>
        <view v-if="isSmallBlind" class="badge sb">SB</view>
        <view v-if="isBigBlind" class="badge bb">BB</view>
        <view v-if="isAllIn" class="badge allin">ALL</view>
      </view>

      <!-- 信息 -->
      <view class="player-info">
        <text class="nickname">{{ player.nickname || '玩家' }}</text>
        <text class="chips-text">{{ formatChips(player.chips) }}</text>
      </view>

      <!-- 下注金额 -->
      <view v-if="player.currentBet > 0" class="bet-display">
        <text>{{ formatChips(player.currentBet) }}</text>
      </view>

      <!-- 弃牌遮罩 -->
      <view v-if="isFolded" class="folded-mask">
        <text>弃</text>
      </view>

      <!-- 离线遮罩 -->
      <view v-if="!player.isOnline" class="offline-mask">
        <text>离线</text>
      </view>
    </template>
  </view>
</template>

<script setup lang="ts">
import { computed } from 'vue'

interface PlayerInfo {
  userId: number
  nickname?: string
  avatar?: string
  chips: number
  currentBet: number
  status: number // 0:等待 1:轮到我 2:弃牌 3:全押 4:筹码不足
  isDealer?: boolean
  isSmallBlind?: boolean
  isBigBlind?: boolean
  isOnline?: boolean
}

const props = withDefaults(defineProps<{
  seatIndex: number    // 座位号 0-8
  player?: PlayerInfo  // 玩家信息，为空表示空位
  isActive?: boolean   // 是否当前操作玩家
}>(), {
  isActive: false
})

// 是否为空位
const isEmpty = computed(() => !props.player)

// 是否弃牌
const isFolded = computed(() => props.player?.status === 2)

// 是否全押
const isAllIn = computed(() => props.player?.status === 3)

// 是否是庄家/小盲/大盲
const isDealer = computed(() => props.player?.isDealer)
const isSmallBlind = computed(() => props.player?.isSmallBlind)
const isBigBlind = computed(() => props.player?.isBigBlind)

// 座位样式类
const seatClasses = computed(() => ({
  'is-empty': isEmpty.value,
  'is-active': props.isActive && !isEmpty.value,
  'is-folded': isFolded.value,
  'is-allin': isAllIn.value,
  'is-offline': !props.player?.isOnline
}))

// 座位位置（9人布局：顶部2、左边2、右边2、底部3）
// 底部3人(4,5,6) - 左边2人(2,3) - 右边2人(7,8) - 顶部2人(0,1)
const seatPositions: Record<number, { top: string; left: string; transform: string }> = {
  0: { top: '10rpx', left: '35%', transform: 'translateX(-50%)' },      // 顶部左
  1: { top: '10rpx', left: '65%', transform: 'translateX(-50%)' },      // 顶部右
  2: { top: '180rpx', left: '2%', transform: 'translateX(0)' },         // 左边上
  3: { top: '380rpx', left: '2%', transform: 'translateX(0)' },         // 左边下
  4: { top: '560rpx', left: '15%', transform: 'translateX(0)' },        // 底部左
  5: { top: '560rpx', left: '50%', transform: 'translateX(-50%)' },     // 底部中（自己）
  6: { top: '560rpx', left: '85%', transform: 'translateX(-100%)' },    // 底部右
  7: { top: '380rpx', left: '98%', transform: 'translateX(-100%)' },    // 右边下
  8: { top: '180rpx', left: '98%', transform: 'translateX(-100%)' }     // 右边上
}

const seatStyle = computed(() => {
  const pos = seatPositions[props.seatIndex] || seatPositions[0]
  return {
    top: pos.top,
    left: pos.left,
    transform: pos.transform
  }
})

// 格式化筹码
const formatChips = (chips: number): string => {
  if (chips >= 10000) {
    return (chips / 10000).toFixed(1) + 'w'
  }
  if (chips >= 1000) {
    return (chips / 1000).toFixed(1) + 'k'
  }
  return chips.toString()
}
</script>

<style scoped>
.player-seat {
  position: absolute;
  width: 90rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 8rpx;
  background: rgba(0, 0, 0, 0.7);
  border-radius: 12rpx;
  border: 2rpx solid transparent;
  transition: all 0.3s ease;
}

/* 当前操作玩家 - 金色边框动画 */
.player-seat.is-active {
  border-color: #ffd700;
  box-shadow: 0 0 16rpx rgba(255, 215, 0, 0.6);
  animation: pulse 1.5s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { box-shadow: 0 0 16rpx rgba(255, 215, 0, 0.6); }
  50% { box-shadow: 0 0 24rpx rgba(255, 215, 0, 0.9); }
}

/* 弃牌状态 */
.player-seat.is-folded {
  opacity: 0.5;
}

/* 空座位 */
.player-seat.is-empty {
  background: transparent;
  border: 2rpx dashed rgba(255, 255, 255, 0.2);
}

.empty-seat {
  width: 100%;
  height: 60rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-text {
  color: rgba(255, 255, 255, 0.3);
  font-size: 20rpx;
}

/* 头像 */
.avatar-wrapper {
  position: relative;
}

.avatar {
  width: 48rpx;
  height: 48rpx;
  border-radius: 50%;
  border: 2rpx solid rgba(255, 255, 255, 0.3);
}

.avatar-default {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
}

.avatar-text {
  color: #ffffff;
  font-size: 22rpx;
  font-weight: bold;
}

/* 徽章 */
.badge {
  position: absolute;
  font-size: 14rpx;
  padding: 2rpx 6rpx;
  border-radius: 6rpx;
  color: #ffffff;
  font-weight: bold;
}

.badge.dealer {
  background: #f39c12;
  top: -6rpx;
  right: -6rpx;
}

.badge.sb {
  background: #3498db;
  bottom: -6rpx;
  left: -6rpx;
}

.badge.bb {
  background: #9b59b6;
  bottom: -6rpx;
  right: -6rpx;
}

.badge.allin {
  background: #e74c3c;
  top: -16rpx;
  left: 50%;
  transform: translateX(-50%);
  white-space: nowrap;
}

/* 玩家信息 */
.player-info {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-top: 4rpx;
}

.nickname {
  color: #ffffff;
  font-size: 18rpx;
  max-width: 80rpx;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.chips-text {
  color: #ffd700;
  font-size: 16rpx;
}

/* 下注显示 */
.bet-display {
  margin-top: 4rpx;
  padding: 2rpx 8rpx;
  background: #f39c12;
  border-radius: 8rpx;
}

.bet-display text {
  color: #ffffff;
  font-size: 16rpx;
  font-weight: bold;
}

/* 弃牌遮罩 */
.folded-mask {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  border-radius: 12rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.folded-mask text {
  color: rgba(255, 255, 255, 0.8);
  font-size: 20rpx;
}

/* 离线遮罩 */
.offline-mask {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  border-radius: 12rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.offline-mask text {
  color: rgba(255, 255, 255, 0.6);
  font-size: 18rpx;
}
</style>
