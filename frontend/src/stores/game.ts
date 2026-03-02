import { defineStore } from 'pinia'
import { ref } from 'vue'

interface Card {
  suit: number  // 花色
  rank: number  // 点数
}

interface PlayerState {
  userId: number
  seatIndex: number
  chips: number
  status: number
  currentBet: number
  cards: Card[]
}

export const useGameStore = defineStore('game', () => {
  const roomCode = ref<string>('')  // 房间号
  const phase = ref<number>(0)  // 游戏阶段
  const pot = ref<number>(0)    // 底池
  const communityCards = ref<Card[]>([])  // 公共牌
  const currentPlayerId = ref<number>(0)  // 当前操作玩家
  const myCards = ref<Card[]>([])  // 我的手牌
  const players = ref<PlayerState[]>([])  // 所有玩家状态
  const dealerPosition = ref<number>(0)  // 庄家位置
  const smallBlindPosition = ref<number>(0)  // 小盲位置
  const bigBlindPosition = ref<number>(0)  // 大盲位置

  const setPhase = (newPhase: number) => {
    phase.value = newPhase
  }

  const setPot = (newPot: number) => {
    pot.value = newPot
  }

  const setCommunityCards = (cards: Card[]) => {
    communityCards.value = cards
  }

  const setMyCards = (cards: Card[]) => {
    myCards.value = cards
  }

  const setCurrentPlayer = (userId: number) => {
    currentPlayerId.value = userId
  }

  const updatePlayer = (userId: number, updates: Partial<PlayerState>) => {
    const index = players.value.findIndex(p => p.userId === userId)
    if (index !== -1) {
      players.value[index] = { ...players.value[index], ...updates }
    }
  }

  const clear = () => {
    roomCode.value = ''
    phase.value = 0
    pot.value = 0
    communityCards.value = []
    currentPlayerId.value = 0
    myCards.value = []
    players.value = []
  }

  return {
    roomCode,
    phase,
    pot,
    communityCards,
    currentPlayerId,
    myCards,
    players,
    dealerPosition,
    smallBlindPosition,
    bigBlindPosition,
    setPhase,
    setPot,
    setCommunityCards,
    setMyCards,
    setCurrentPlayer,
    updatePlayer,
    clear
  }
})
