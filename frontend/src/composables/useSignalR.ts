import { ref } from 'vue'
import * as signalR from '@microsoft/signalr'
import { useGameStore, type GameStateDto, type GameResultDto } from '@/stores/game'
import {
  useZjhGameStore,
  type ZjhGameStateDto,
  type ZjhGameResultDto,
  type ZjhAvailableActionsDto,
  type ZjhPlayerHandResultDto
} from '@/stores/zjhGame'
import { useUserStore } from '@/stores/user'
import { useRoomStore } from '@/stores/room'

// 全局单例连接状态
let globalConnection: signalR.HubConnection | null = null
let globalIsConnected = false
let globalError: string | null = null
let isConnecting = false
let eventListenersSetup = false

export const useSignalR = () => {
  const connection = ref<signalR.HubConnection | null>(globalConnection)
  const isConnected = ref(globalIsConnected)
  const error = ref(globalError)

  const gameStore = useGameStore()
  const zjhGameStore = useZjhGameStore()
  const userStore = useUserStore()
  const roomStore = useRoomStore()

  // 同步全局状态到 ref
  const syncState = () => {
    connection.value = globalConnection
    isConnected.value = globalIsConnected
    error.value = globalError
  }

  // 连接 SignalR（单例模式）
  const connect = async () => {
    const token = uni.getStorageSync('token')

    if (!token) {
      globalError = '未登录'
      syncState()
      return false
    }

    // 如果已经连接，直接返回
    if (globalConnection && globalIsConnected) {
      console.log('[SignalR] 复用现有连接')
      syncState()
      return true
    }

    // 如果正在连接中，等待连接完成
    if (isConnecting) {
      console.log('[SignalR] 等待现有连接完成...')
      // 简单等待机制
      let attempts = 0
      while (isConnecting && attempts < 50) {
        await new Promise(resolve => setTimeout(resolve, 100))
        attempts++
      }
      syncState()
      return globalIsConnected
    }

    isConnecting = true

    try {
      // SignalR Hub 地址 - 统一使用远程服务器
      const hubUrl = 'http://8.137.12.241:9001/gameHub'

      globalConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: () => 3000 // 3秒重连
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      // 注册事件监听（只注册一次）
      if (!eventListenersSetup) {
        setupEventListeners()
        eventListenersSetup = true
      }

      // 连接状态监听
      globalConnection.onclose(() => {
        globalIsConnected = false
        console.log('[SignalR] 连接已关闭')
        syncState()
      })

      globalConnection.onreconnected(() => {
        globalIsConnected = true
        console.log('[SignalR] 重连成功')
        // 重连后重新加入房间
        if (gameStore.roomCode) {
          joinRoom(gameStore.roomCode)
        }
        syncState()
      })

      globalConnection.onreconnecting(() => {
        console.log('[SignalR] 正在重连...')
      })

      await globalConnection.start()
      globalIsConnected = true
      globalError = null
      console.log('[SignalR] 连接成功')

      // 设置当前用户ID
      if (userStore.userInfo?.id) {
        gameStore.setMyUserId(userStore.userInfo.id)
      }

      syncState()
      return true
    } catch (e: any) {
      globalError = e.message
      globalIsConnected = false
      console.error('[SignalR] 连接失败:', e.message)
      syncState()
      return false
    } finally {
      isConnecting = false
    }
  }

  // 设置事件监听（全局只设置一次）
  const setupEventListeners = () => {
    if (!globalConnection) return

    // 错误消息
    globalConnection.on('Error', (message: string) => {
      console.error('[SignalR] Error:', message)
      uni.showToast({ title: message, icon: 'none' })
    })

    // 玩家加入
    globalConnection.on('PlayerJoined', (data: { UserId: number; ConnectionId: string; Timestamp: string }) => {
      console.log('[SignalR] PlayerJoined:', data)
      // 可以在这里更新房间玩家列表
    })

    // 玩家离开
    globalConnection.on('PlayerLeft', (data: { UserId: number; ConnectionId: string; Timestamp: string }) => {
      console.log('[SignalR] PlayerLeft:', data)
    })

    // 玩家断开连接
    globalConnection.on('PlayerDisconnected', (data: { UserId: number; Timestamp: string }) => {
      console.log('[SignalR] PlayerDisconnected:', data)
      uni.showToast({ title: '有玩家断开连接', icon: 'none' })
    })

    // 玩家准备
    globalConnection.on('PlayerReady', (data: { UserId: number; Success: boolean; Message: string; Timestamp: string }) => {
      console.log('[SignalR] PlayerReady:', data)
      if (data.Success) {
        uni.showToast({ title: data.Message, icon: 'none' })
      }
    })

    // 游戏开始
    globalConnection.on('GameStarted', (data: { Message: string }) => {
      console.log('[SignalR] GameStarted:', data)
      uni.showToast({ title: '游戏开始！', icon: 'success' })
    })

    // 游戏重置（服务重启后状态丢失）
    globalConnection.on('GameReset', (data: { Message: string; RoomCode: string }) => {
      console.log('[SignalR] GameReset:', data)
      uni.showModal({
        title: '提示',
        content: data.Message || '游戏状态已重置，请重新开始游戏',
        showCancel: false,
        success: () => {
          // lobby 是 tabbar 页面，必须用 switchTab
          uni.switchTab({ url: '/pages/lobby/lobby' })
        }
      })
    })

    // 房间解散（房主离开）
    globalConnection.on('RoomDismissed', (data: { Message: string; RoomCode: string }) => {
      console.log('[SignalR] RoomDismissed:', data)
      uni.showModal({
        title: '房间已解散',
        content: data.Message || '房主已解散房间',
        showCancel: false,
        success: () => {
          uni.switchTab({ url: '/pages/lobby/lobby' })
        }
      })
    })

    // 玩家被踢出（筹码不足）
    globalConnection.on('PlayerRemoved', (data: { UserId: number; Reason: string; Timestamp: string }) => {
      console.log('[SignalR] PlayerRemoved:', data)
      // 检查是否是自己被踢出
      if (data.UserId === userStore.userInfo?.id) {
        // 设置被踢出状态，在游戏结束时处理
        roomStore.setRemoved(data.Reason || '筹码不足，已自动离开房间')
      } else {
        // 其他玩家被踢出，显示提示
        uni.showToast({ title: data.Reason || '有玩家因筹码不足离开', icon: 'none' })
      }
    })

    // 房间玩家列表更新
    globalConnection.on('RoomPlayersUpdated', (players: any[]) => {
      console.log('[SignalR] RoomPlayersUpdated:', players)
      roomStore.updatePlayers(players)
    })

    // 游戏状态更新（核心事件）
    globalConnection.on('GameStateUpdated', (state: GameStateDto) => {
      console.log('[SignalR] GameStateUpdated:', state)
      gameStore.updateFromServer(state)
    })

    // 游戏结束
    globalConnection.on('GameEnded', (result: GameResultDto) => {
      console.log('[SignalR] GameEnded:', result)
      gameStore.setGameResult(result)
    })

    // ========== 扎金花游戏事件 ==========
    const zjhGameStore = useZjhGameStore()

    // 扎金花游戏开始
    globalConnection.on('ZjhGameStarted', (data: { Message: string }) => {
      console.log('[SignalR] ZjhGameStarted:', data)
      uni.showToast({ title: '游戏开始！', icon: 'success' })
    })

    // 扎金花游戏状态更新
    globalConnection.on('ZjhGameStateUpdated', (state: ZjhGameStateDto) => {
      console.log('[SignalR] ZjhGameStateUpdated:', state)
      zjhGameStore.updateFromServer(state)
    })

    // 扎金花游戏结束
    globalConnection.on('ZjhGameEnded', (result: ZjhGameResultDto) => {
      console.log('[SignalR] ZjhGameEnded:', result)
      zjhGameStore.setGameResult(result)
    })

    // 扎金花可用操作
    globalConnection.on('ZjhAvailableActions', (actions: ZjhAvailableActionsDto) => {
      console.log('[SignalR] ZjhAvailableActions:', actions)
      zjhGameStore.setAvailableActions(actions)
    })

    // 扎金花比牌输家看牌
    globalConnection.on('ZjhCompareLose', (result: ZjhPlayerHandResultDto) => {
      console.log('[SignalR] ZjhCompareLose:', result)
      zjhGameStore.setCompareLoseResult(result)
    })
  }

  // 断开连接（仅在真正需要时调用，如退出登录）
  const disconnect = async () => {
    if (globalConnection) {
      try {
        await globalConnection.stop()
      } catch (e) {
        // ignore
      }
      globalConnection = null
      globalIsConnected = false
      eventListenersSetup = false
      syncState()
    }
  }

  // 加入房间
  const joinRoom = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      const connected = await connect()
      if (!connected) return
    }

    try {
      await globalConnection!.invoke('JoinRoom', roomCode)
      console.log('[SignalR] Joined room:', roomCode)
    } catch (e: any) {
      console.error('[SignalR] JoinRoom failed:', e.message)
      globalError = e.message
      syncState()
    }
  }

  // 离开房间
  const leaveRoom = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) return

    try {
      await globalConnection.invoke('LeaveRoom', roomCode)
      console.log('[SignalR] Left room:', roomCode)
    } catch (e: any) {
      console.error('[SignalR] LeaveRoom failed:', e.message)
    }
  }

  // 准备/取消准备
  const readyGame = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      const connected = await connect()
      if (!connected) {
        uni.showToast({ title: '连接失败，请重试', icon: 'none' })
        return
      }
    }
    try {
      await globalConnection!.invoke('ReadyGame', roomCode)
      console.log('[SignalR] ReadyGame success')
    } catch (e: any) {
      console.error('[SignalR] ReadyGame failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 开始游戏
  const startGame = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) return
    try {
      await globalConnection.invoke('StartGame', roomCode)
    } catch (e: any) {
      console.error('[SignalR] StartGame failed:', e.message)
    }
  }

  // 弃牌
  const fold = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('Fold', roomCode)
    } catch (e: any) {
      console.error('[SignalR] Fold failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 过牌
  const check = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('Check', roomCode)
    } catch (e: any) {
      console.error('[SignalR] Check failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 跟注/下注
  const bet = async (roomCode: string, amount: number) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('Bet', roomCode, amount)
    } catch (e: any) {
      console.error('[SignalR] Bet failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 加注
  const raise = async (roomCode: string, amount: number) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('Raise', roomCode, amount)
    } catch (e: any) {
      console.error('[SignalR] Raise failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 全押
  const allIn = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('AllIn', roomCode)
    } catch (e: any) {
      console.error('[SignalR] AllIn failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // ========== 扎金花游戏方法 ==========

  // 开始扎金花游戏
  const startZjhGame = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      const connected = await connect()
      if (!connected) return
    }
    try {
      await globalConnection!.invoke('StartZjhGame', roomCode)
    } catch (e: any) {
      console.error('[SignalR] StartZjhGame failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 扎金花-看牌
  const zjhLook = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('ZjhLook', roomCode)
    } catch (e: any) {
      console.error('[SignalR] ZjhLook failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 扎金花-下注
  const zjhBet = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('ZjhBet', roomCode)
    } catch (e: any) {
      console.error('[SignalR] ZjhBet failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 扎金花-加注
  const zjhRaise = async (roomCode: string, newBetAmount: number) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('ZjhRaise', roomCode, newBetAmount)
    } catch (e: any) {
      console.error('[SignalR] ZjhRaise failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 扎金花-比牌
  const zjhCompare = async (roomCode: string, targetUserId: number) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('ZjhCompare', roomCode, targetUserId)
    } catch (e: any) {
      console.error('[SignalR] ZjhCompare failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 扎金花-弃牌
  const zjhFold = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('ZjhFold', roomCode)
    } catch (e: any) {
      console.error('[SignalR] ZjhFold failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 扎金花-全押
  const zjhAllIn = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) {
      uni.showToast({ title: '连接已断开', icon: 'none' })
      return
    }
    try {
      await globalConnection.invoke('ZjhAllIn', roomCode)
    } catch (e: any) {
      console.error('[SignalR] ZjhAllIn failed:', e.message)
      uni.showToast({ title: e.message || '操作失败', icon: 'none' })
    }
  }

  // 扎金花-获取可用操作
  const getZjhAvailableActions = async (roomCode: string) => {
    if (!globalConnection || !globalIsConnected) return
    try {
      await globalConnection.invoke('GetZjhAvailableActions', roomCode)
    } catch (e: any) {
      console.error('[SignalR] GetZjhAvailableActions failed:', e.message)
    }
  }

  return {
    connection,
    isConnected,
    error,
    connect,
    disconnect,
    joinRoom,
    leaveRoom,
    readyGame,
    startGame,
    fold,
    check,
    bet,
    raise,
    allIn,
    // 扎金花
    startZjhGame,
    zjhLook,
    zjhBet,
    zjhRaise,
    zjhCompare,
    zjhFold,
    zjhAllIn,
    getZjhAvailableActions
  }
}
