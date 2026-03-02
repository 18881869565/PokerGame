<template>
  <view
    class="poker-card"
    :class="[sizeClass, { 'face-down': faceDown, 'red': isRed }]"
  >
    <!-- 正面 -->
    <template v-if="!faceDown">
      <view class="card-corner top-left">
        <text class="rank">{{ rankDisplay }}</text>
        <text class="suit">{{ suitSymbol }}</text>
      </view>
      <view class="card-center">
        <text class="suit-large">{{ suitSymbol }}</text>
      </view>
      <view class="card-corner bottom-right">
        <text class="rank">{{ rankDisplay }}</text>
        <text class="suit">{{ suitSymbol }}</text>
      </view>
    </template>
    <!-- 背面 -->
    <template v-else>
      <view class="card-back">
        <view class="card-back-pattern"></view>
      </view>
    </template>
  </view>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  suit: number       // 0:♠ 1:♥ 2:♣ 3:♦
  rank: number       // 2-14: 2-10,J,Q,K,A
  faceDown?: boolean // 背面朝上
  size?: 'small' | 'medium' | 'large'
}>(), {
  faceDown: false,
  size: 'medium'
})

// 花色符号
const suitSymbols = ['♠', '♥', '♣', '♦']

// 点数显示
const rankSymbols = ['', '', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K', 'A']

const suitSymbol = computed(() => suitSymbols[props.suit] || '?')
const rankDisplay = computed(() => rankSymbols[props.rank] || '?')
const isRed = computed(() => props.suit === 1 || props.suit === 3) // 红桃或方块
const sizeClass = computed(() => `size-${props.size}`)
</script>

<style scoped>
.poker-card {
  background: #ffffff;
  border-radius: 8rpx;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.15);
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1rpx solid #e0e0e0;
}

/* 尺寸 */
.size-small {
  width: 60rpx;
  height: 84rpx;
}

.size-medium {
  width: 80rpx;
  height: 112rpx;
}

.size-large {
  width: 100rpx;
  height: 140rpx;
}

/* 红色花色 */
.poker-card.red .rank,
.poker-card.red .suit,
.poker-card.red .suit-large {
  color: #e74c3c;
}

/* 黑色花色 */
.poker-card:not(.red) .rank,
.poker-card:not(.red) .suit,
.poker-card:not(.red) .suit-large {
  color: #1a1a1a;
}

/* 角落（左上） */
.card-corner.top-left {
  position: absolute;
  top: 4rpx;
  left: 6rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  line-height: 1;
}

/* 角落（右下） */
.card-corner.bottom-right {
  position: absolute;
  bottom: 4rpx;
  right: 6rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  line-height: 1;
  transform: rotate(180deg);
}

/* 点数 */
.rank {
  font-weight: bold;
}

.size-small .rank {
  font-size: 18rpx;
}

.size-medium .rank {
  font-size: 24rpx;
}

.size-large .rank {
  font-size: 28rpx;
}

/* 花色 */
.suit {
  font-size: 14rpx;
}

.size-medium .suit {
  font-size: 18rpx;
}

.size-large .suit {
  font-size: 22rpx;
}

/* 中央花色 */
.card-center {
  display: flex;
  align-items: center;
  justify-content: center;
}

.suit-large {
  font-size: 32rpx;
}

.size-small .suit-large {
  font-size: 24rpx;
}

.size-medium .suit-large {
  font-size: 36rpx;
}

.size-large .suit-large {
  font-size: 48rpx;
}

/* 背面 */
.poker-card.face-down {
  background: linear-gradient(135deg, #1a5276 0%, #2980b9 100%);
  border: none;
}

.card-back {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.card-back-pattern {
  width: 70%;
  height: 80%;
  border: 2rpx solid rgba(255, 255, 255, 0.3);
  border-radius: 4rpx;
  background: repeating-linear-gradient(
    45deg,
    transparent,
    transparent 4rpx,
    rgba(255, 255, 255, 0.1) 4rpx,
    rgba(255, 255, 255, 0.1) 8rpx
  );
}
</style>
