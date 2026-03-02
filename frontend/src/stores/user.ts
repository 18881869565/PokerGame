import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

interface UserInfo {
  id: number
  username: string
  nickname: string
  chips: number
  avatar?: string
}

export const useUserStore = defineStore('user', () => {
  const token = ref<string>('')
  const userInfo = ref<UserInfo | null>(null)

  const isLoggedIn = computed(() => !!token.value)
  const nickname = computed(() => userInfo.value?.nickname || '')
  const chips = computed(() => userInfo.value?.chips || 0)

  const setToken = (newToken: string) => {
    token.value = newToken
    uni.setStorageSync('token', newToken)
  }

  const setUser = (user: UserInfo) => {
    userInfo.value = user
    uni.setStorageSync('userInfo', JSON.stringify(user))
  }

  const updateChips = (newChips: number) => {
    if (userInfo.value) {
      userInfo.value.chips = newChips
      uni.setStorageSync('userInfo', JSON.stringify(userInfo.value))
    }
  }

  const logout = () => {
    token.value = ''
    userInfo.value = null
    uni.removeStorageSync('token')
    uni.removeStorageSync('userInfo')
  }

  const init = () => {
    const savedToken = uni.getStorageSync('token')
    const savedUserInfo = uni.getStorageSync('userInfo')
    if (savedToken) {
      token.value = savedToken
    }
    if (savedUserInfo) {
      try {
        userInfo.value = JSON.parse(savedUserInfo)
      } catch (e) {
        console.error('解析用户信息失败', e)
      }
    }
  }

  return {
    token,
    userInfo,
    isLoggedIn,
    nickname,
    chips,
    setToken,
    setUser,
    updateChips,
    logout,
    init
  }
})
