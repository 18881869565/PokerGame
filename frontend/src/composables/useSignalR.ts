import { ref, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'
import { useGameStore, type GameStateDto, type GameResultDto } from '@/stores/game'
import { useUserStore } from '@/stores/user'

export const useSignalR = () => {
  const connection = ref<signalR.HubConnection | null>(null)
  const isConnected = ref(false)
  const error = ref<string | null>(null)

  const gameStore = useGameStore()
  const userStore = useUserStore()

  // 连接 SignalR
  const connect = async () => {
    const token = uni.getStorageSync('token')

    if (!token) {
      error.value = '未登录'
      return false
    }

    try {
      // 关闭旧连接
      if (connection.value) {
        await connection.value.stop()
      }

      connection.value = new signalR.HubConnectionBuilder()
        .withUrl('/gameHub', {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: () => 3000 // 3秒重连
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      // 注册事件监听
      setupEventListeners()

      // 连接状态监听
      connection.value.onclose(() => {
        isConnected.value = false
        console.log('[SignalR] 连接已关闭')
      })

      connection.value.onreconnected(() => {
        isConnected.value = true
        console.log('[SignalR] 重连成功')
        // 重连后重新加入房间
        if (gameStore.roomCode) {
          joinRoom(gameStore.roomCode)
        }
      })

      connection.value.onreconnecting(() => {
        console.log('[SignalR] 正在重连...')
      })

      await connection.value.start()
      isConnected.value = true
      error.value = null
      console.log('[SignalR] 连接成功')

      // 设置当前用户ID
      if (userStore.userInfo?.id) {
        gameStore.setMyUserId(userStore.userInfo.id)
      }

      return true
    } catch (e: any) {
      error.value = e.message
      isConnected.value = false
      console.error('[SignalR] 连接失败:', e.message)
      return false
    }
  }

  // 设置事件监听
  const setupEventListeners = () => {
    if (!connection.value) return

    // 错误消息
    connection.value.on('Error', (message: string) => {
      console.error('[SignalR] Error:', message)
      uni.showToast({ title: message, icon: 'none' })
    })

    // 玩家加入
    connection.value.on('PlayerJoined', (data: { UserId: number; ConnectionId: string; Timestamp: string }) => {
      console.log('[SignalR] PlayerJoined:', data)
      // 可以在这里更新房间玩家列表
    })

    // 玩家离开
    connection.value.on('PlayerLeft', (data: { UserId: number; ConnectionId: string; Timestamp: string }) => {
      console.log('[SignalR] PlayerLeft:', data)
    })

    // 玩家断开连接
    connection.value.on('PlayerDisconnected', (data: { UserId: number; Timestamp: string }) => {
      console.log('[SignalR] PlayerDisconnected:', data)
      uni.showToast({ title: '有玩家断开连接', icon: 'none' })
    })

    // 玩家准备
    connection.value.on('PlayerReady', (data: { UserId: number; Success: boolean; Message: string; Timestamp: string }) => {
      console.log('[SignalR] PlayerReady:', data)
      if (data.Success) {
        uni.showToast({ title: data.Message, icon: 'none' })
      }
    })

    // 游戏开始
    connection.value.on('GameStarted', (data: { Message: string }) => {
      console.log('[SignalR] GameStarted:', data)
      uni.showToast({ title: '游戏开始！', icon: 'success' })
    })

    // 游戏状态更新（核心事件）
    connection.value.on('GameStateUpdated', (state: GameStateDto) => {
      console.log('[SignalR] GameStateUpdated:', state)
      gameStore.updateFromServer(state)
    })

    // 游戏结束
    connection.value.on('GameEnded', (result: GameResultDto) => {
      console.log('[SignalR] GameEnded:', result)
      gameStore.setGameResult(result)
    })
  }

  // 断开连接
  const disconnect = async () => {
    if (connection.value) {
      try {
        await connection.value.stop()
      } catch (e) {
        // ignore
      }
      connection.value = null
      isConnected.value = false
    }
  }

  // 加入房间
  const joinRoom = async (roomCode: string) => {
    if (!connection.value || !isConnected.value) {
      const connected = await connect()
      if (!connected) return
    }

    try {
      await connection.value!.invoke('JoinRoom', roomCode)
      console.log('[SignalR] Joined room:', roomCode)
    } catch (e: any) {
      console.error('[SignalR] JoinRoom failed:', e.message)
      error.value = e.message
    }
  }

  // 离开房间
  const leaveRoom = async (roomCode: string) => {
    if (!connection.value || !isConnected.value) return

    try {
      await connection.value.invoke('LeaveRoom', roomCode)
      console.log('[SignalR] Left room:', roomCode)
    } catch (e: any) {
      console.error('[SignalR] LeaveRoom failed:', e.message)
    }
  }

  // 准备/取消准备
  const readyGame = async (roomCode: string) => {
    if (!connection.value || !isConnected.value) return
    try {
      await connection.value.invoke('ReadyGame', roomCode)
    } catch (e: any) {
      console.error('[SignalR] ReadyGame failed:', e.message)
    }
  }

  // 开始游戏
  const startGame = async (roomCode: string) => {
    if (!connection.value || !isConnected.value) return
    try {
      await connection.value.invoke('StartGame', roomCode)
    } catch (e: any) {
      console.error('[SignalR] StartGame failed:', e.message)
    }
  }

  // 弃牌
  const fold = async (roomCode: string) => {
    if (!connection.value || !isConnected.value) return
    try {
      await connection.value.invoke('Fold', roomCode)
    } catch (e: any) {
      console.error('[SignalR] Fold failed:', e.message)
    }
  }

  // 过牌
  const check = async (roomCode: string) => {
    if (!connection.value || !isConnected.value) return
    try {
      await connection.value.invoke('Check', roomCode)
    } catch (e: any) {
      console.error('[SignalR] Check failed:', e.message)
    }
  }

  // 跟注/下注
  const bet = async (roomCode: string, amount: number) => {
    if (!connection.value || !isConnected.value) return
    try {
      await connection.value.invoke('Bet', roomCode, amount)
    } catch (e: any) {
      console.error('[SignalR] Bet failed:', e.message)
    }
  }

  // 加注
  const raise = async (roomCode: string, amount: number) => {
    if (!connection.value || !isConnected.value) return
    try {
      await connection.value.invoke('Raise', roomCode, amount)
    } catch (e: any) {
      console.error('[SignalR] Raise failed:', e.message)
    }
  }

  // 全押
  const allIn = async (roomCode: string) => {
    if (!connection.value || !isConnected.value) return
    try {
      await connection.value.invoke('AllIn', roomCode)
    } catch (e: any) {
      console.error('[SignalR] AllIn failed:', e.message)
    }
  }

  // 组件卸载时断开连接
  onUnmounted(() => {
    // 注意：不要在全局 composable 中自动断开
    // 由页面自行决定何时断开
  })

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
    allIn
  }
}
