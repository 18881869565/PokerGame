import { ref, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'

export const useSignalR = () => {
  const connection = ref<signalR.HubConnection | null>(null)
  const isConnected = ref(false)
  const error = ref<string | null>(null)

  const connect = async () => {
    const token = uni.getStorageSync('token')

    try {
      connection.value = new signalR.HubConnectionBuilder()
        .withUrl('/gameHub', {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build()

      connection.value.onclose(() => {
        isConnected.value = false
      })

      connection.value.onreconnected(() => {
        isConnected.value = true
      })

      await connection.value.start()
      isConnected.value = true
      error.value = null
    } catch (e: any) {
      error.value = e.message
      isConnected.value = false
    }
  }

  const disconnect = async () => {
    if (connection.value) {
      await connection.value.stop()
      connection.value = null
      isConnected.value = false
    }
  }

  const joinRoom = async (roomCode: string) => {
    if (connection.value && isConnected.value) {
      await connection.value.invoke('JoinRoom', roomCode)
    }
  }

  const leaveRoom = async (roomCode: string) => {
    if (connection.value && isConnected.value) {
      await connection.value.invoke('LeaveRoom', roomCode)
    }
  }

  const readyGame = async (roomCode: string) => {
    if (connection.value && isConnected.value) {
      await connection.value.invoke('ReadyGame', roomCode)
    }
  }

  const bet = async (roomCode: string, amount: number) => {
    if (connection.value && isConnected.value) {
      await connection.value.invoke('Bet', roomCode, amount)
    }
  }

  const fold = async (roomCode: string) => {
    if (connection.value && isConnected.value) {
      await connection.value.invoke('Fold', roomCode)
    }
  }

  const check = async (roomCode: string) => {
    if (connection.value && isConnected.value) {
      await connection.value.invoke('Check', roomCode)
    }
  }

  const raise = async (roomCode: string, amount: number) => {
    if (connection.value && isConnected.value) {
      await connection.value.invoke('Raise', roomCode, amount)
    }
  }

  const allIn = async (roomCode: string) => {
    if (connection.value && isConnected.value) {
      await connection.value.invoke('AllIn', roomCode)
    }
  }

  const on = (eventName: string, callback: (...args: any[]) => void) => {
    if (connection.value) {
      connection.value.on(eventName, callback)
    }
  }

  const off = (eventName: string, callback: (...args: any[]) => void) => {
    if (connection.value) {
      connection.value.off(eventName, callback)
    }
  }

  onUnmounted(() => {
    disconnect()
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
    bet,
    fold,
    check,
    raise,
    allIn,
    on,
    off
  }
}
