<template>
  <view class="pot-display" :class="{ 'has-pot': pot > 0 }">
    <!-- 筹码堆 -->
    <view v-if="pot > 0" class="pot-chips">
      <ChipStack
        :amount="pot"
        :max-chips="6"
        size="large"
        :show-amount="false"
      />
    </view>

    <!-- 底池金额 -->
    <view class="pot-info">
      <text class="pot-label">底池</text>
      <text class="pot-amount">{{ formattedPot }}</text>
    </view>

    <!-- 边池（如果有） -->
    <view v-if="sidePots && sidePots.length > 0" class="side-pots">
      <view
        v-for="(sidePot, index) in sidePots"
        :key="index"
        class="side-pot"
      >
        <text class="side-pot-label">边池 {{ index + 1 }}</text>
        <text class="side-pot-amount">{{ formatAmount(sidePot) }}</text>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import ChipStack from './ChipStack.vue'

const props = withDefaults(defineProps<{
  pot: number           // 主池金额
  sidePots?: number[]   // 边池金额数组
  animated?: boolean    // 是否显示动画
}>(), {
  sidePots: () => [],
  animated: false
})

// 格式化主池金额
const formattedPot = computed(() => formatAmount(props.pot))

// 格式化金额
const formatAmount = (amount: number): string => {
  if (amount >= 10000) {
    return (amount / 10000).toFixed(1) + '万'
  }
  return amount.toString()
}
</script>

<style scoped>
.pot-display {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16rpx 24rpx;
  background: rgba(0, 0, 0, 0.5);
  border-radius: 16rpx;
  border: 1rpx solid rgba(255, 215, 0, 0.2);
  transition: all 0.3s ease;
}

.pot-display.has-pot {
  border-color: rgba(255, 215, 0, 0.5);
  box-shadow: 0 0 20rpx rgba(255, 215, 0, 0.2);
}

/* 筹码区域 */
.pot-chips {
  margin-bottom: 8rpx;
}

/* 底池信息 */
.pot-info {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.pot-label {
  color: rgba(255, 255, 255, 0.7);
  font-size: 22rpx;
  margin-bottom: 4rpx;
}

.pot-amount {
  color: #ffd700;
  font-size: 32rpx;
  font-weight: bold;
  text-shadow: 0 2rpx 4rpx rgba(0, 0, 0, 0.5);
}

/* 边池 */
.side-pots {
  margin-top: 12rpx;
  padding-top: 12rpx;
  border-top: 1rpx solid rgba(255, 255, 255, 0.1);
  width: 100%;
}

.side-pot {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 4rpx 0;
}

.side-pot-label {
  color: rgba(255, 255, 255, 0.5);
  font-size: 20rpx;
}

.side-pot-amount {
  color: #3498db;
  font-size: 22rpx;
  font-weight: bold;
}
</style>
