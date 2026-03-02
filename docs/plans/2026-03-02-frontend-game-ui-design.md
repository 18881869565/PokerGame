# 前端游戏界面设计文档

> 创建日期：2026-03-02

## 一、设计概述

### 设计风格
- **主题**：经典绿色牌桌风格
- **参考**：真实赌场，绿色椭圆形桌面，木质边框

### 技术方案
- **扑克牌**：纯 CSS/SVG 绘制，不依赖图片资源
- **座位布局**：固定 9 座位布局，空位显示"空位"
- **操作按钮**：底部固定按钮栏，不可用时置灰

---

## 二、组件结构

```
frontend/src/components/
├── PokerCard.vue      # 扑克牌组件（CSS绘制）
├── PlayerSeat.vue     # 玩家座位组件
├── ChipStack.vue      # 筹码堆组件
├── ActionBar.vue      # 操作按钮栏
└── PotDisplay.vue     # 底池显示组件
```

---

## 三、PokerCard 组件

### 外观规格（基于 750px 设计稿）

| 尺寸 | 宽度 | 高度 | 字体大小 | 用途 |
|------|------|------|----------|------|
| small | 60rpx | 84rpx | 20rpx | 公共牌 |
| medium | 80rpx | 112rpx | 28rpx | 其他玩家手牌 |
| large | 100rpx | 140rpx | 36rpx | 我的手牌 |

### 花色映射

```typescript
const suitSymbols = ['♠', '♥', '♣', '♦']
const suitColors = ['#1a1a1a', '#e74c3c', '#1a1a1a', '#e74c3c']
const rankSymbols = ['', '', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K', 'A']
```

### Props 接口

```typescript
defineProps<{
  suit: number       // 0-3: ♠♥♣♦
  rank: number       // 2-14
  faceDown?: boolean // 背面朝上
  size?: 'small' | 'medium' | 'large'
}>()
```

---

## 四、PlayerSeat 组件

### 座位布局（9人固定位置）

| 座位 | 位置 | top | left | 说明 |
|------|------|-----|------|------|
| 0 | 顶部中央 | 20rpx | 50% | |
| 1 | 右上 | 120rpx | 85% | |
| 2 | 右侧上 | 320rpx | 92% | |
| 3 | 右侧下 | 520rpx | 92% | |
| 4 | 右下 | 680rpx | 75% | |
| 5 | 底部右 | 680rpx | 25% | 底部留给自己 |
| 6 | 左下 | 680rpx | 25% | |
| 7 | 左侧下 | 520rpx | 8% | |
| 8 | 左侧上 | 320rpx | 8% | |

### 座位状态

- **空位**：灰色虚线边框 + "空位" 文字
- **等待中**：玩家头像 + 昵称 + 筹码
- **游戏中**：额外显示下注金额、状态标识
- **当前操作**：金色边框动画
- **已弃牌**：半透明 + 灰色遮罩
- **全押**：红色"ALL IN"标签

---

## 五、ActionBar 组件

### 按钮状态

| 按钮 | 颜色 | 可用条件 |
|------|------|----------|
| 弃牌 | 灰色 #666 | 始终可用 |
| 过牌 | 蓝色 #3498db | 当前下注 = 我的下注 |
| 跟注 | 绿色 #27ae60 | 当前下注 > 我的下注 |
| 加注 | 橙色 #f39c12 | 筹码充足 |
| 全押 | 红色 #e74c3c | 始终可用 |

### 加注滑块

- 最小值：当前最高下注 + 大盲注
- 最大值：我的全部筹码
- 快捷按钮：2x、3x、底池

---

## 六、SignalR 事件

### 服务端推送事件（前端监听）

| 事件名 | 数据结构 | 处理逻辑 |
|--------|----------|----------|
| `Error` | `string` | 显示错误 toast |
| `PlayerJoined` | `{ UserId, ConnectionId, Timestamp }` | 更新玩家列表 |
| `PlayerLeft` | `{ UserId, ConnectionId, Timestamp }` | 移除玩家 |
| `PlayerDisconnected` | `{ UserId, Timestamp }` | 标记玩家离线 |
| `PlayerReady` | `{ UserId, Success, Message, Timestamp }` | 更新准备状态 |
| `GameStarted` | `{ Message }` | 显示提示 |
| `GameStateUpdated` | `GameStateDto` | 更新 gameStore |
| `GameEnded` | `GameResultDto` | 显示结算弹窗 |

### 前端调用方法

| 方法名 | 参数 | 说明 |
|--------|------|------|
| `JoinRoom` | `roomCode` | 加入房间 |
| `LeaveRoom` | `roomCode` | 离开房间 |
| `ReadyGame` | `roomCode` | 准备/取消 |
| `StartGame` | `roomCode` | 开始游戏 |
| `Fold` | `roomCode` | 弃牌 |
| `Check` | `roomCode` | 过牌 |
| `Bet` | `roomCode, amount` | 跟注 |
| `Raise` | `roomCode, amount` | 加注 |
| `AllIn` | `roomCode` | 全押 |

---

## 七、GameStore 增强

### 新增状态

```typescript
const currentHighestBet = ref(0)  // 当前最高下注
const smallBlind = ref(10)        // 小盲注
const bigBlind = ref(20)          // 大盲注
const dealerId = ref(0)           // 庄家用户ID
```

### 新增方法

```typescript
const updateFromServer = (data: GameStateDto) => {
  // 批量更新状态
}

const reset = () => {
  // 重置所有状态
}
```

### 计算属性

```typescript
const isMyTurn = computed(() => currentPlayerId === myUserId)
const canCheck = computed(() => currentHighestBet === myCurrentBet)
const callAmount = computed(() => currentHighestBet - myCurrentBet)
const minRaise = computed(() => currentHighestBet + bigBlind)
```
