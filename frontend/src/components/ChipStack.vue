<template>
  <view class="chip-stack" :class="sizeClass">
    <!-- 筹码堆叠效果 -->
    <view class="chips-container">
      <view
        v-for="(chip, index) in displayChips"
        :key="index"
        class="chip"
        :class="chip.colorClass"
        :style="{ bottom: (index * stackOffset) + 'rpx' }"
      />
    </view>
    <!-- 金额显示 -->
    <view v-if="showAmount" class="amount-badge">
      <text class="amount-text">{{ formattedAmount }}</text>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  amount: number          // 筹码金额
  maxChips?: number       // 最大显示筹码数量
  size?: 'small' | 'medium' | 'large'  // 尺寸
  showAmount?: boolean    // 是否显示金额
}>(), {
  maxChips: 5,
  size: 'medium',
  showAmount: true
})

// 尺寸类
const sizeClass = computed(() => `size-${props.size}`)

// 根据金额确定筹码颜色组合
interface ChipColor {
  value: number
  colorClass: string
}

const chipColors: ChipColor[] = [
  { value: 10000, colorClass: 'chip-gold' },      // 金色 10000
  { value: 5000, colorClass: 'chip-purple' },     // 紫色 5000
  { value: 1000, colorClass: 'chip-yellow' },     // 黄色 1000
  { value: 500, colorClass: 'chip-black' },       // 黑色 500
  { value: 100, colorClass: 'chip-blue' },        // 蓝色 100
  { value: 50, colorClass: 'chip-green' },        // 绿色 50
  { value: 25, colorClass: 'chip-red' },          // 红色 25
  { value: 5, colorClass: 'chip-white' }          // 白色 5
]

// 计算需要显示的筹码
const displayChips = computed(() => {
  const chips: { colorClass: string }[] = []
  let remaining = props.amount
  let chipCount = 0

  for (const chipColor of chipColors) {
    if (remaining <= 0 || chipCount >= props.maxChips) break

    const count = Math.floor(remaining / chipColor.value)
    if (count > 0) {
      // 从大到小添加筹码
      for (let i = 0; i < Math.min(count, props.maxChips - chipCount); i++) {
        chips.unshift({ colorClass: chipColor.colorClass })
        chipCount++
        if (chipCount >= props.maxChips) break
      }
      remaining -= count * chipColor.value
    }
  }

  // 如果没有筹码，显示一个默认的
  if (chips.length === 0 && props.amount > 0) {
    chips.push({ colorClass: 'chip-white' })
  }

  return chips
})

// 堆叠偏移量
const stackOffset = computed(() => {
  switch (props.size) {
    case 'small': return 3
    case 'large': return 6
    default: return 4
  }
})

// 格式化金额
const formattedAmount = computed(() => {
  if (props.amount >= 10000) {
    return (props.amount / 10000).toFixed(1) + '万'
  }
  return props.amount.toString()
})
</script>

<style scoped>
.chip-stack {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
}

/* 尺寸变体 */
.chip-stack.size-small .chip {
  width: 32rpx;
  height: 32rpx;
}

.chip-stack.size-medium .chip {
  width: 40rpx;
  height: 40rpx;
}

.chip-stack.size-large .chip {
  width: 50rpx;
  height: 50rpx;
}

/* 筹码容器 */
.chips-container {
  position: relative;
  height: 60rpx;
  display: flex;
  align-items: flex-end;
}

/* 单个筹码 */
.chip {
  position: absolute;
  border-radius: 50%;
  border: 2rpx solid rgba(255, 255, 255, 0.3);
  box-shadow:
    inset 0 2rpx 4rpx rgba(255, 255, 255, 0.3),
    inset 0 -2rpx 4rpx rgba(0, 0, 0, 0.2),
    0 2rpx 4rpx rgba(0, 0, 0, 0.3);
}

/* 筹码颜色 */
.chip-white {
  background: linear-gradient(135deg, #ffffff 0%, #e0e0e0 100%);
}

.chip-red {
  background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%);
}

.chip-green {
  background: linear-gradient(135deg, #27ae60 0%, #1e8449 100%);
}

.chip-blue {
  background: linear-gradient(135deg, #3498db 0%, #2980b9 100%);
}

.chip-black {
  background: linear-gradient(135deg, #2c3e50 0%, #1a252f 100%);
}

.chip-yellow {
  background: linear-gradient(135deg, #f1c40f 0%, #d4ac0d 100%);
}

.chip-purple {
  background: linear-gradient(135deg, #9b59b6 0%, #8e44ad 100%);
}

.chip-gold {
  background: linear-gradient(135deg, #f39c12 0%, #d68910 100%);
  box-shadow:
    inset 0 2rpx 4rpx rgba(255, 255, 255, 0.5),
    inset 0 -2rpx 4rpx rgba(0, 0, 0, 0.2),
    0 2rpx 6rpx rgba(243, 156, 18, 0.5);
}

/* 金额徽章 */
.amount-badge {
  margin-top: 8rpx;
  padding: 2rpx 10rpx;
  background: rgba(0, 0, 0, 0.7);
  border-radius: 10rpx;
  border: 1rpx solid rgba(255, 215, 0, 0.5);
}

.amount-text {
  color: #ffd700;
  font-size: 20rpx;
  font-weight: bold;
}

.size-large .amount-badge {
  padding: 4rpx 14rpx;
}

.size-large .amount-text {
  font-size: 24rpx;
}
</style>
