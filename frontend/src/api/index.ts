// API 基础地址 - 统一使用远程服务器
const BASE_URL = 'http://8.137.12.241:9001/api'

interface RequestOptions {
  url: string
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  data?: any
  header?: Record<string, string>
}

interface ApiResponse<T = any> {
  success: boolean
  message: string
  data?: T
  code?: number
}

export const request = async <T = any>(options: RequestOptions): Promise<ApiResponse<T>> => {
  const token = uni.getStorageSync('token')

  return new Promise((resolve, reject) => {
    uni.request({
      url: BASE_URL + options.url,
      method: options.method || 'GET',
      data: options.data,
      header: {
        'Content-Type': 'application/json',
        'Authorization': token ? `Bearer ${token}` : '',
        ...options.header
      },
      success: (res: any) => {
        if (res.statusCode === 200) {
          resolve(res.data as ApiResponse<T>)
        } else if (res.statusCode === 401) {
          // 未授权，跳转登录
          uni.removeStorageSync('token')
          uni.removeStorageSync('userInfo')
          uni.navigateTo({ url: '/pages/login/login' })
          reject(new Error('未授权'))
        } else if (res.statusCode >= 400 && res.statusCode < 500) {
          // 业务错误，返回响应数据让调用方处理
          resolve(res.data as ApiResponse<T>)
        } else {
          reject(new Error(res.data?.message || '请求失败'))
        }
      },
      fail: (err) => {
        reject(new Error(err.errMsg || '网络错误'))
      }
    })
  })
}

// 认证相关 API
export const authApi = {
  register: (data: { username: string; password: string; nickname?: string }) =>
    request({ url: '/auth/register', method: 'POST', data }),

  login: (data: { username: string; password: string }) =>
    request<{ token: string; userId: number }>({ url: '/auth/login', method: 'POST', data })
}

// 用户相关 API
export const userApi = {
  getProfile: () =>
    request({ url: '/user/profile' }),

  updateProfile: (data: { nickname?: string; avatar?: string }) =>
    request({ url: '/user/profile', method: 'PUT', data }),

  dailyGift: () =>
    request<{ chips: number }>({ url: '/user/daily-gift', method: 'POST' })
}

// 房间相关 API
export const roomApi = {
  create: (data: { maxPlayers: number; smallBlind: number; bigBlind: number; bringChips?: number }) =>
    request<{ roomCode: string; roomId: number }>({ url: '/room/create', method: 'POST', data }),

  getRoom: (roomCode: string) =>
    request({ url: `/room/${roomCode}` }),

  joinRoom: (roomCode: string, bringChips: number = 0) =>
    request({ url: '/room/join', method: 'POST', data: { roomCode, bringChips } }),

  getQRCode: (roomCode: string) =>
    request<{ QRCodeUrl: string }>({ url: `/room/qrcode/${roomCode}` }),

  changeSeat: (roomId: number, seatIndex: number) =>
    request({ url: `/room/${roomId}/changeseat`, method: 'POST', data: { seatIndex } })
}

// 好友相关 API
export const friendApi = {
  sendRequest: (target: string) =>
    request({ url: '/friend/request', method: 'POST', data: { target } }),

  acceptRequest: (id: number) =>
    request({ url: `/friend/accept/${id}`, method: 'POST' }),

  rejectRequest: (id: number) =>
    request({ url: `/friend/reject/${id}`, method: 'POST' }),

  deleteFriend: (id: number) =>
    request({ url: `/friend/${id}`, method: 'DELETE' }),

  getList: () =>
    request({ url: '/friend/list' })
}
