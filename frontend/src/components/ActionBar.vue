<template>
  <view class="action-bar" :class="{ disabled: !isMyTurn }">
    <!-- 基础按钮 -->
    <button class="action-btn fold" :disabled="!isMyTurn" @click="handleFold">
      弃牌
    </button>

    <!-- 全押模式：只能全押或弃牌 -->
    <template v-if="isAllInMode">
      <button class="action-btn allin" :disabled="!isMyTurn" @click="handleAllIn">
        全押 {{ myChips > 0 ? myChips : '' }}
      </button>
    </template>

    <!-- 正常模式：所有操作都可用 -->
    <template v-else>
      <button
        class="action-btn check"
        :disabled="!isMyTurn || !canCheck"
        @click="handleCheck"
      >
        过牌
      </button>

      <button
        class="action-btn call"
        :disabled="!isMyTurn || canCheck"
        @click="handleCall"
      >
        跟注 {{ callAmount > 0 ? callAmount : '' }}
      </button>

      <button
        class="action-btn raise"
        :disabled="!isMyTurn || !canRaise"
        @click="showRaisePanel = true"
      >
        加注
      </button>

      <button class="action-btn allin" :disabled="!isMyTurn" @click="handleAllIn">
        全押
      </button>
    </template>

    <!-- 加注面板 -->
    <view v-if="showRaisePanel" class="raise-panel" @click.stop>
      <view class="raise-header">
        <text class="raise-title">选择加注金额</text>
        <text class="raise-current">当前: {{ raiseAmount }}</text>
      </view>

      <!-- 滑块 -->
      <slider
        :min="minRaise"
        :max="maxRaise"
        :value="raiseAmount"
        @changing="onSliderChange"
        @change="onSliderChange"
        activeColor="#f39c12"
        backgroundColor="#ddd"
        block-size="24"
      />

      <!-- 快捷按钮 -->
      <view class="quick-buttons">
        <button
          v-for="quick in quickRaises"
          :key="quick.label"
          class="quick-btn"
          @click="setRaiseAmount(quick.value)"
        >
          {{ quick.label }}
        </button>
      </view>

      <!-- 操作按钮 -->
      <view class="raise-actions">
        <button class="cancel-btn" @click="showRaisePanel = false">取消</button>
        <button class="confirm-btn" @click="confirmRaise">确认加注</button>
      </view>
    </view>

    <!-- 遮罩 -->
    <view v-if="showRaisePanel" class="overlay" @click="showRaisePanel = false"></view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'

const props = withDefaults(defineProps<{
  isMyTurn: boolean
  canCheck: boolean
  currentHighestBet: number
  myCurrentBet: number
  myChips: number
  bigBlind: number
  pot: number
  hasAllInPlayer: boolean  // 是否有玩家已全押
}>(), {
  isMyTurn: false,
  canCheck: true,
  currentHighestBet: 0,
  myCurrentBet: 0,
  myChips: 0,
  bigBlind: 20,
  pot: 0,
  hasAllInPlayer: false
})

const emit = defineEmits<{
  fold: []
  check: []
  call: []
  raise: [amount: number]
  allIn: []
}>()

// 跟注金额
const callAmount = computed(() => props.currentHighestBet - props.myCurrentBet)

// 全押模式：当有玩家已全押时，其他玩家只能全押或弃牌
const isAllInMode = computed(() => props.hasAllInPlayer)

// 能否加注
const canRaise = computed(() => props.myChips > callAmount.value && !props.hasAllInPlayer)

// 最小加注
const minRaise = computed(() => props.currentHighestBet + props.bigBlind)

// 最大加注（全押）
const maxRaise = computed(() => props.myChips + props.myCurrentBet)

// 加注金额
const raiseAmount = ref(minRaise.value)

// 监听最小加注变化，重置加注金额
watch(minRaise, (val) => {
  raiseAmount.value = val
})

// 显示加注面板
const showRaisePanel = ref(false)

// 快捷加注选项
const quickRaises = computed(() => [
  { label: '2x', value: Math.min(props.currentHighestBet * 2, maxRaise.value) },
  { label: '3x', value: Math.min(props.currentHighestBet * 3, maxRaise.value) },
  { label: '底池', value: Math.min(props.pot, maxRaise.value) },
  { label: '1/2底池', value: Math.min(Math.floor(props.pot / 2), maxRaise.value) }
])

// 滑块变化
const onSliderChange = (e: any) => {
  raiseAmount.value = e.detail.value
}

// 设置加注金额
const setRaiseAmount = (value: number) => {
  raiseAmount.value = Math.max(minRaise.value, Math.min(value, maxRaise.value))
}

// 操作处理
const handleFold = () => {
  if (props.isMyTurn) emit('fold')
}

const handleCheck = () => {
  if (props.isMyTurn && props.canCheck) emit('check')
}

const handleCall = () => {
  if (props.isMyTurn && !props.canCheck) emit('call')
}

const handleAllIn = () => {
  if (props.isMyTurn) emit('allIn')
}

const confirmRaise = () => {
  // 加注金额 = 实际下注总额
  const actualRaise = raiseAmount.value - props.myCurrentBet
  emit('raise', actualRaise)
  showRaisePanel.value = false
}
</script>

<style scoped>
.action-bar {
  position: fixed;
  bottom: 40rpx;
  left: 0;
  right: 0;
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 16rpx;
  padding: 0 20rpx;
  z-index: 100;
}

.action-bar.disabled {
  opacity: 0.5;
}

/* 按钮 */
.action-btn {
  padding: 20rpx 28rpx;
  border-radius: 12rpx;
  font-size: 26rpx;
  font-weight: bold;
  color: #ffffff;
  border: none;
  min-width: 100rpx;
}

.action-btn::after {
  border: none;
}

.action-btn[disabled] {
  opacity: 0.4;
}

.action-btn.fold {
  background: #666666;
}

.action-btn.check {
  background: #3498db;
}

.action-btn.call {
  background: #27ae60;
}

.action-btn.raise {
  background: #f39c12;
}

.action-btn.allin {
  background: #e74c3c;
}

/* 加注面板 */
.raise-panel {
  position: fixed;
  bottom: 140rpx;
  left: 50%;
  transform: translateX(-50%);
  width: 600rpx;
  background: #ffffff;
  border-radius: 20rpx;
  padding: 30rpx;
  z-index: 1001;
  box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.3);
}

.raise-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30rpx;
}

.raise-title {
  font-size: 30rpx;
  font-weight: bold;
  color: #333333;
}

.raise-current {
  font-size: 28rpx;
  color: #f39c12;
  font-weight: bold;
}

/* 快捷按钮 */
.quick-buttons {
  display: flex;
  gap: 16rpx;
  margin-top: 24rpx;
  margin-bottom: 24rpx;
}

.quick-btn {
  flex: 1;
  padding: 16rpx 0;
  background: #f5f5f5;
  border-radius: 8rpx;
  font-size: 24rpx;
  color: #333333;
  border: 1rpx solid #ddd;
}

.quick-btn::after {
  border: none;
}

/* 操作按钮 */
.raise-actions {
  display: flex;
  gap: 20rpx;
  margin-top: 20rpx;
}

.cancel-btn,
.confirm-btn {
  flex: 1;
  padding: 20rpx 0;
  border-radius: 12rpx;
  font-size: 28rpx;
  font-weight: bold;
  border: none;
}

.cancel-btn::after,
.confirm-btn::after {
  border: none;
}

.cancel-btn {
  background: #f5f5f5;
  color: #666666;
}

.confirm-btn {
  background: #f39c12;
  color: #ffffff;
}

/* 遮罩 */
.overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 1000;
}
</style>
