import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

// 扎金花游戏阶段枚举
export enum ZjhGamePhase {
  Waiting = 0,
  Dealing = 1,
  Betting = 2,
  Comparing = 3,
  Finished = 4
}

// 扎金花玩家操作枚举
export enum ZjhAction {
  Look = 1,
  BetBlind = 2,
  BetLook = 3,
  Compare = 4,
  Fold = 5,
  AllIn = 6
}

// 卡牌接口
export interface Card {
  suit: number   // 花色 0-3
  rank: number   // 点数 2-14
  displayName?: string
}

// 扎金花玩家接口
export interface ZjhPlayer {
  userId: number
  username: string
  nickname: string
  seatIndex: number
  chips: number
  totalBet: number
  hasLooked: boolean
  isFolded: boolean
  isOut: boolean
  isCompareLose: boolean
  lastAction?: ZjhAction
  hand?: Card[]  // 只有看过的牌或游戏结束时可见
  isInGame: boolean
  betMultiplier: number
  isOnline?: boolean  // 是否在线
}

// 扎金花游戏状态接口
export interface ZjhGameStateDto {
  phase: ZjhGamePhase
  pot: number
  betAmount: number
  anteAmount: number
  roundCount: number
  maxRounds: number
  currentPlayerId: number | null
  players: ZjhPlayer[]
  winnerId: number | null
}

// 扎金花游戏结果接口
export interface ZjhGameResultDto {
  winnerId: number
  winnerNickname: string
  pot: number
  handDescription?: string
  playerHands: {
    userId: number
    nickname: string
    hand: Card[]
    handDescription?: string
    isWinner: boolean
  }[]
}

// 扎金花可用操作接口
export interface ZjhAvailableActionsDto {
  actions: ZjhAction[]
  minBetAmount: number
  compareCost: number
}

// 扎金花玩家手牌结果接口（用于比牌输家看牌）
export interface ZjhPlayerHandResultDto {
  userId: number
  nickname: string
  hand: Card[]
  handDescription?: string
  isWinner: boolean
}

export const useZjhGameStore = defineStore('zjhGame', () => {
  // ========== 状态 ==========
  const roomCode = ref<string>('')
  const roomId = ref<number>(0)
  const phase = ref<ZjhGamePhase>(ZjhGamePhase.Waiting)
  const pot = ref<number>(0)
  const betAmount = ref<number>(1)
  const anteAmount = ref<number>(1)
  const roundCount = ref<number>(0)
  const maxRounds = ref<number>(20)
  const currentPlayerId = ref<number | null>(null)
  const players = ref<ZjhPlayer[]>([])
  const winnerId = ref<number | null>(null)
  const myUserId = ref<number>(0)

  // 游戏结果
  const gameResult = ref<ZjhGameResultDto | null>(null)

  // 可用操作
  const availableActions = ref<ZjhAvailableActionsDto | null>(null)

  // 比牌输家结果（给没看过牌的输家看）
  const compareLoseResult = ref<ZjhPlayerHandResultDto | null>(null)

  // ========== 计算属性 ==========
  // 当前用户
  const currentUser = computed(() =>
    players.value.find(p => p.userId === myUserId.value)
  )

  // 是否轮到我
  const isMyTurn = computed(() =>
    currentPlayerId.value === myUserId.value &&
    phase.value === ZjhGamePhase.Betting
  )

  // 我的筹码
  const myChips = computed(() => currentUser.value?.chips ?? 0)

  // 我是否已看牌
  const hasLooked = computed(() => currentUser.value?.hasLooked ?? false)

  // 我的下注倍率
  const myBetMultiplier = computed(() => currentUser.value?.betMultiplier ?? 1)

  // 我需要支付的下注金额
  const myBetCost = computed(() => betAmount.value * myBetMultiplier.value)

  // 比牌需要支付的金额
  const myCompareCost = computed(() => betAmount.value * myBetMultiplier.value * 2)

  // 存活玩家数量
  const activePlayerCount = computed(() =>
    players.value.filter(p => p.isInGame).length
  )

  // 游戏阶段描述
  const phaseText = computed(() => {
    switch (phase.value) {
      case ZjhGamePhase.Waiting: return '等待开始'
      case ZjhGamePhase.Dealing: return '发牌中'
      case ZjhGamePhase.Betting: return `第 ${roundCount.value} 轮`
      case ZjhGamePhase.Comparing: return '比牌中'
      case ZjhGamePhase.Finished: return '游戏结束'
      default: return ''
    }
  })

  // 我的手牌
  const myHand = computed(() => {
    const me = currentUser.value
    // 只有看过的牌或游戏结束时才能看到
    if (me && (me.hasLooked || phase.value === ZjhGamePhase.Finished)) {
      return me.hand || []
    }
    return []
  })

  // ========== 方法 ==========
  // 设置当前用户ID
  const setMyUserId = (userId: number) => {
    myUserId.value = userId
  }

  // 设置房间信息
  const setRoom = (code: string, id: number) => {
    roomCode.value = code
    roomId.value = id
  }

  // 从服务器更新游戏状态
  const updateFromServer = (data: ZjhGameStateDto) => {
    console.log('[ZjhGameStore] updateFromServer:', data)
    phase.value = data.phase
    pot.value = data.pot
    betAmount.value = data.betAmount
    anteAmount.value = data.anteAmount
    roundCount.value = data.roundCount
    maxRounds.value = data.maxRounds
    currentPlayerId.value = data.currentPlayerId
    winnerId.value = data.winnerId
    // 确保 isOnline 字段有默认值
    players.value = data.players.map(p => ({
      ...p,
      isOnline: p.isOnline ?? true  // 默认在线
    }))
    // 后端会自动发送 ZjhAvailableActions 事件，无需手动请求
  }

  // 设置游戏结果
  const setGameResult = (result: ZjhGameResultDto) => {
    gameResult.value = result
  }

  // 设置可用操作
  const setAvailableActions = (actions: ZjhAvailableActionsDto) => {
    availableActions.value = actions
  }

  // 设置比牌输家结果
  const setCompareLoseResult = (result: ZjhPlayerHandResultDto | null) => {
    compareLoseResult.value = result
  }

  // 清除游戏结果
  const clearGameResult = () => {
    gameResult.value = null
  }

  // 重置
  const reset = () => {
    roomCode.value = ''
    roomId.value = 0
    phase.value = ZjhGamePhase.Waiting
    pot.value = 0
    betAmount.value = 1
    anteAmount.value = 1
    roundCount.value = 0
    maxRounds.value = 20
    currentPlayerId.value = null
    players.value = []
    winnerId.value = null
    gameResult.value = null
    availableActions.value = null
    compareLoseResult.value = null
  }

  return {
    // 状态
    roomCode,
    roomId,
    phase,
    pot,
    betAmount,
    anteAmount,
    roundCount,
    maxRounds,
    currentPlayerId,
    players,
    winnerId,
    myUserId,
    gameResult,
    availableActions,
    compareLoseResult,
    // 计算属性
    currentUser,
    isMyTurn,
    myChips,
    hasLooked,
    myBetMultiplier,
    myBetCost,
    myCompareCost,
    activePlayerCount,
    phaseText,
    myHand,
    // 方法
    setMyUserId,
    setRoom,
    updateFromServer,
    setGameResult,
    setAvailableActions,
    setCompareLoseResult,
    clearGameResult,
    reset
  }
})
