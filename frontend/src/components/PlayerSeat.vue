<template>
  <view
    class="player-seat"
    :class="seatClasses"
    :style="seatStyle"
  >
    <!-- 空座位 -->
    <template v-if="isEmpty">
      <view class="empty-seat">
        <text class="empty-text">空位</text>
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
        <view v-if="isAllIn" class="badge allin">ALL IN</view>
      </view>

      <!-- 信息 -->
      <view class="player-info">
        <text class="nickname">{{ player.nickname || '玩家' }}</text>
        <text class="chips">💰 {{ formatChips(player.chips) }}</text>
      </view>

      <!-- 下注金额 -->
      <view v-if="player.currentBet > 0" class="bet-display">
        <text>{{ formatChips(player.currentBet) }}</text>
      </view>

      <!-- 弃牌遮罩 -->
      <view v-if="isFolded" class="folded-mask">
        <text>已弃牌</text>
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

// 座位位置（固定9座位布局）
const seatPositions: Record<number, { top: string; left: string; transform: string }> = {
  0: { top: '20rpx', left: '50%', transform: 'translateX(-50%)' },
  1: { top: '120rpx', left: '85%', transform: 'translateX(-50%)' },
  2: { top: '320rpx', left: '92%', transform: 'translateX(-50%)' },
  3: { top: '520rpx', left: '92%', transform: 'translateX(-50%)' },
  4: { top: '680rpx', left: '75%', transform: 'translateX(-50%)' },
  5: { top: '680rpx', left: '55%', transform: 'translateX(-50%)' }, // 底部右侧（自己）
  6: { top: '680rpx', left: '45%', transform: 'translateX(-50%)' }, // 底部左侧（预留）
  7: { top: '520rpx', left: '8%', transform: 'translateX(-50%)' },
  8: { top: '320rpx', left: '8%', transform: 'translateX(-50%)' }
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
    return (chips / 10000).toFixed(1) + '万'
  }
  return chips.toString()
}
</script>

<style scoped>
.player-seat {
  position: absolute;
  width: 120rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12rpx;
  background: rgba(0, 0, 0, 0.6);
  border-radius: 16rpx;
  border: 2rpx solid transparent;
  transition: all 0.3s ease;
}

/* 当前操作玩家 - 金色边框动画 */
.player-seat.is-active {
  border-color: #ffd700;
  box-shadow: 0 0 20rpx rgba(255, 215, 0, 0.6);
  animation: pulse 1.5s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { box-shadow: 0 0 20rpx rgba(255, 215, 0, 0.6); }
  50% { box-shadow: 0 0 30rpx rgba(255, 215, 0, 0.9); }
}

/* 弃牌状态 */
.player-seat.is-folded {
  opacity: 0.5;
}

/* 空座位 */
.player-seat.is-empty {
  background: transparent;
  border: 2rpx dashed rgba(255, 255, 255, 0.3);
}

.empty-seat {
  width: 100%;
  height: 80rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-text {
  color: rgba(255, 255, 255, 0.4);
  font-size: 24rpx;
}

/* 头像 */
.avatar-wrapper {
  position: relative;
  margin-bottom: 8rpx;
}

.avatar {
  width: 64rpx;
  height: 64rpx;
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
  font-size: 28rpx;
  font-weight: bold;
}

/* 徽章 */
.badge {
  position: absolute;
  font-size: 18rpx;
  padding: 2rpx 8rpx;
  border-radius: 8rpx;
  color: #ffffff;
  font-weight: bold;
}

.badge.dealer {
  background: #f39c12;
  top: -8rpx;
  right: -8rpx;
}

.badge.sb {
  background: #3498db;
  bottom: -8rpx;
  left: -8rpx;
}

.badge.bb {
  background: #9b59b6;
  bottom: -8rpx;
  right: -8rpx;
}

.badge.allin {
  background: #e74c3c;
  top: -20rpx;
  left: 50%;
  transform: translateX(-50%);
  white-space: nowrap;
}

/* 玩家信息 */
.player-info {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.nickname {
  color: #ffffff;
  font-size: 22rpx;
  max-width: 100rpx;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.chips {
  color: #ffd700;
  font-size: 20rpx;
}

/* 下注显示 */
.bet-display {
  margin-top: 6rpx;
  padding: 4rpx 12rpx;
  background: #e74c3c;
  border-radius: 12rpx;
  color: #ffffff;
  font-size: 20rpx;
  font-weight: bold;
}

/* 弃牌遮罩 */
.folded-mask {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.folded-mask text {
  color: rgba(255, 255, 255, 0.8);
  font-size: 24rpx;
}

/* 离线遮罩 */
.offline-mask {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.offline-mask text {
  color: rgba(255, 255, 255, 0.6);
  font-size: 22rpx;
}
</style>
