import { defineStore } from 'pinia'
import { ref } from 'vue'

interface Player {
  id: number
  userId: number
  nickname: string
  seatIndex: number
  chips: number
  isReady: boolean
  isOnline: boolean
  isOwner?: boolean
}

export const useRoomStore = defineStore('room', () => {
  const roomCode = ref<string>('')
  const roomId = ref<number>(0)
  const ownerId = ref<number>(0)
  const maxPlayers = ref<number>(9)
  const smallBlind = ref<number>(10)
  const bigBlind = ref<number>(20)
  const status = ref<number>(0)
  const players = ref<Player[]>([])
  const wasRemoved = ref<boolean>(false)  // 是否被踢出房间（筹码不足）
  const removeReason = ref<string>('')    // 被踢出的原因

  const setRoom = (room: {
    roomCode: string
    roomId: number
    ownerId: number
    maxPlayers: number
    smallBlind: number
    bigBlind: number
    status: number
  }) => {
    roomCode.value = room.roomCode
    roomId.value = room.roomId
    ownerId.value = room.ownerId
    maxPlayers.value = room.maxPlayers
    smallBlind.value = room.smallBlind
    bigBlind.value = room.bigBlind
    status.value = room.status
  }

  const setPlayers = (playerList: Player[]) => {
    players.value = playerList
  }

  const updatePlayers = (playerList: Player[]) => {
    players.value = playerList
  }

  const addPlayer = (player: Player) => {
    players.value.push(player)
  }

  const removePlayer = (userId: number) => {
    players.value = players.value.filter(p => p.userId !== userId)
  }

  const setRemoved = (reason: string) => {
    wasRemoved.value = true
    removeReason.value = reason
  }

  const clearRemoved = () => {
    wasRemoved.value = false
    removeReason.value = ''
  }

  const clear = () => {
    roomCode.value = ''
    roomId.value = 0
    ownerId.value = 0
    players.value = []
    wasRemoved.value = false
    removeReason.value = ''
  }

  return {
    roomCode,
    roomId,
    ownerId,
    maxPlayers,
    smallBlind,
    bigBlind,
    status,
    players,
    wasRemoved,
    removeReason,
    setRoom,
    setPlayers,
    updatePlayers,
    addPlayer,
    removePlayer,
    setRemoved,
    clearRemoved,
    clear
  }
})
