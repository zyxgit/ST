import axios, { AxiosError, AxiosHeaders } from 'axios'

import { clearTokens, getAccessToken, getRefreshToken, setTokens } from '@/auth/token'
import { useDiscrete } from '@/lib/naive'
import router from '@/router'
import type { LoginResult } from '@/types/auth'

const API_BASE = import.meta.env.VITE_API_BASE_URL
/** 网关路由要求路径以 /api 开头，拼接后的 baseURL 形如 http://host:port/api */
const GATEWAY_URL = API_BASE ? `${API_BASE.replace(/\/+$/, '')}/api` : '/api'

const instance = axios.create({
  baseURL: GATEWAY_URL,
  timeout: 20000,
})

function buildApiUrl(path: string) {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${GATEWAY_URL}${normalizedPath}`
}

let refreshPromise: Promise<LoginResult> | null = null

instance.interceptors.request.use((config) => {
  const token = getAccessToken()

  if (token) {
    if (typeof config.headers?.set === 'function') {
      config.headers.set('Authorization', `Bearer ${token}`)
    } else {
      config.headers = AxiosHeaders.from({
        ...config.headers,
        Authorization: `Bearer ${token}`,
      })
    }
  }

  return config
})

instance.interceptors.response.use(
  (response) => response.data,
  async (error: AxiosError<{ message?: string; detail?: string; title?: string }>) => {
    const { message, dialog } = useDiscrete()
    const originalRequest = error.config
    const status = error.response?.status

    // 429 限流处理
    if (status === 429) {
      // silent 模式下不弹 dialog，由调用方自行处理重试
      if (!(originalRequest as any)?.silent) {
        const retryAfter = parseInt(error.response?.headers?.['retry-after'] || '60', 10)

        dialog.warning({
          title: '操作太频繁',
          content: `请求过于频繁，请在 ${retryAfter} 秒后重试。`,
          positiveText: '我知道了',
        })
      }

      return Promise.reject(error)
    }

    if (status === 401 && originalRequest && !originalRequest.headers?.['x-refresh-retry']) {
      const refreshToken = getRefreshToken()

      if (!refreshToken) {
        clearTokens()
        await router.replace('/login')
        return Promise.reject(error)
      }

      refreshPromise ??= axios
        .post<LoginResult>(
          buildApiUrl('/identity/user/refresh'),
          { refreshToken },
        )
        .then((response) => response.data)
        .finally(() => {
          refreshPromise = null
        })

      try {
        const tokens = await refreshPromise
        setTokens(tokens)

        if (typeof originalRequest.headers?.set === 'function') {
          originalRequest.headers.set('Authorization', `Bearer ${tokens.accessToken}`)
          originalRequest.headers.set('x-refresh-retry', '1')
        } else {
          originalRequest.headers = AxiosHeaders.from({
            ...originalRequest.headers,
            Authorization: `Bearer ${tokens.accessToken}`,
            'x-refresh-retry': '1',
          })
        }

        return instance(originalRequest)
      } catch (refreshError) {
        clearTokens()
        await router.replace('/login')
        return Promise.reject(refreshError)
      }
    }

    const errorMessage =
      error.response?.data?.message ??
      error.response?.data?.detail ??
      error.response?.data?.title ??
      error.message ??
      '请求失败，请稍后重试'

    message.error(errorMessage)
    return Promise.reject(error)
  },
)

const request = {
  get<T>(url: string, config?: object) {
    return instance.get<T, T>(url, config)
  },
  post<T>(url: string, data?: unknown, config?: object) {
    return instance.post<T, T>(url, data, config)
  },
  put<T>(url: string, data?: unknown, config?: object) {
    return instance.put<T, T>(url, data, config)
  },
  delete<T>(url: string, config?: object) {
    return instance.delete<T, T>(url, config)
  },
}

export default request
