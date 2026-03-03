import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

// 游戏阶段枚举
export enum GamePhase {
  Waiting = 0,
  Starting = 1,
  PreFlop = 2,
  Flop = 3,
  Turn = 4,
  River = 5,
  Showdown = 6,
  Finished = 7
}

// 玩家状态枚举
export enum PlayerStatus {
  Waiting = 0,
  MyTurn = 1,
  Folded = 2,
  AllIn = 3,
  OutOfChips = 4
}

// 卡牌接口
export interface Card {
  suit: number   // 花色 0-3
  rank: number   // 点数 2-14
  displayName?: string
}

// 玩家状态接口
export interface GamePlayer {
  userId: number
  nickname: string
  avatar?: string
  seatIndex: number
  chips: number
  currentBet: number
  status: PlayerStatus
  lastAction?: number
  isDealer: boolean
  isSmallBlind: boolean
  isBigBlind: boolean
  isOnline?: boolean  // 是否在线，默认 true
  holeCards?: Card[]  // 只有自己的牌可见
}

// 游戏状态接口（后端返回）
export interface GameStateDto {
  phase: GamePhase
  communityCards: Card[]
  players: GamePlayer[]
  pot: number
  currentHighestBet: number
  currentPlayerId: number | null
  dealerId: number
  smallBlind: number
  bigBlind: number
  hasAllInPlayer: boolean  // 是否有玩家已全押
}

// 游戏结果接口
export interface GameResultDto {
  winnerIds: number[]
  pot: number
  playerHands: {
    userId: number
    nickname: string
    holeCards: Card[]
    handDescription?: string
    chipsWon: number
  }[]
}

export const useGameStore = defineStore('game', () => {
  // ========== 状态 ==========
  const roomCode = ref<string>('')
  const roomId = ref<number>(0)
  const phase = ref<GamePhase>(GamePhase.Waiting)
  const pot = ref<number>(0)
  const communityCards = ref<Card[]>([])
  const currentPlayerId = ref<number | null>(null)
  const players = ref<GamePlayer[]>([])
  const dealerId = ref<number>(0)
  const smallBlind = ref<number>(10)
  const bigBlind = ref<number>(20)
  const currentHighestBet = ref<number>(0)
  const myUserId = ref<number>(0)
  const hasAllInPlayer = ref<boolean>(false)  // 是否有玩家已全押

  // 游戏结果
  const gameResult = ref<GameResultDto | null>(null)

  // ========== 计算属性 ==========
  // 当前用户
  const currentUser = computed(() =>
    players.value.find(p => p.userId === myUserId.value)
  )

  // 是否轮到我
  const isMyTurn = computed(() =>
    currentPlayerId.value === myUserId.value &&
    phase.value !== GamePhase.Waiting &&
    phase.value !== GamePhase.Finished
  )

  // 我可以过牌吗
  const canCheck = computed(() => {
    const me = currentUser.value
    if (!me) return false
    return me.currentBet >= currentHighestBet.value
  })

  // 跟注金额
  const callAmount = computed(() => {
    const me = currentUser.value
    if (!me) return 0
    return Math.max(0, currentHighestBet.value - me.currentBet)
  })

  // 最小加注金额
  const minRaise = computed(() =>
    currentHighestBet.value + bigBlind.value
  )

  // 我的筹码
  const myChips = computed(() => currentUser.value?.chips ?? 0)

  // 我的当前下注
  const myCurrentBet = computed(() => currentUser.value?.currentBet ?? 0)

  // 游戏阶段描述
  const phaseText = computed(() => {
    switch (phase.value) {
      case GamePhase.Waiting: return '等待开始'
      case GamePhase.Starting: return '游戏开始'
      case GamePhase.PreFlop: return '翻牌前'
      case GamePhase.Flop: return '翻牌'
      case GamePhase.Turn: return '转牌'
      case GamePhase.River: return '河牌'
      case GamePhase.Showdown: return '摊牌'
      case GamePhase.Finished: return '游戏结束'
      default: return ''
    }
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
  const updateFromServer = (data: GameStateDto) => {
    phase.value = data.phase
    communityCards.value = data.communityCards || []
    // 游戏中的玩家默认视为在线（后端不返回 isOnline，但能收到游戏状态说明在线）
    players.value = (data.players || []).map(p => ({
      ...p,
      isOnline: p.isOnline !== false // 默认 true，除非明确设为 false
    }))
    pot.value = data.pot
    currentHighestBet.value = data.currentHighestBet
    currentPlayerId.value = data.currentPlayerId
    dealerId.value = data.dealerId
    smallBlind.value = data.smallBlind
    bigBlind.value = data.bigBlind
    hasAllInPlayer.value = data.hasAllInPlayer || false
  }

  // 设置游戏结果
  const setGameResult = (result: GameResultDto) => {
    gameResult.value = result
    phase.value = GamePhase.Finished
  }

  // 清空游戏结果
  const clearGameResult = () => {
    gameResult.value = null
  }

  // 重置所有状态
  const reset = () => {
    roomCode.value = ''
    roomId.value = 0
    phase.value = GamePhase.Waiting
    pot.value = 0
    communityCards.value = []
    currentPlayerId.value = null
    players.value = []
    dealerId.value = 0
    smallBlind.value = 10
    bigBlind.value = 20
    currentHighestBet.value = 0
    gameResult.value = null
  }

  return {
    // 状态
    roomCode,
    roomId,
    phase,
    pot,
    communityCards,
    currentPlayerId,
    players,
    dealerId,
    smallBlind,
    bigBlind,
    currentHighestBet,
    myUserId,
    hasAllInPlayer,
    gameResult,

    // 计算属性
    currentUser,
    isMyTurn,
    canCheck,
    callAmount,
    minRaise,
    myChips,
    myCurrentBet,
    phaseText,

    // 方法
    setMyUserId,
    setRoom,
    updateFromServer,
    setGameResult,
    clearGameResult,
    reset
  }
})
