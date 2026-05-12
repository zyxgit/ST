import { useStorage } from '@vueuse/core'

interface AuthStorage {
  accessToken: string
  refreshToken: string
  expiresAt: string
  refreshTokenExpiresAt: string
}

type TokenPayload = Partial<AuthStorage> & {
  AccessToken?: string
  RefreshToken?: string
  ExpiresAt?: string
  RefreshTokenExpiresAt?: string
}

const storage = useStorage<AuthStorage>('st-admin-auth', {
  accessToken: '',
  refreshToken: '',
  expiresAt: '',
  refreshTokenExpiresAt: '',
})

export function getAccessToken() {
  return storage.value.accessToken
}

export function getRefreshToken() {
  return storage.value.refreshToken
}

export function setTokens(payload: TokenPayload) {
  const normalized: Partial<AuthStorage> = {
    accessToken: payload.accessToken ?? payload.AccessToken ?? '',
    refreshToken: payload.refreshToken ?? payload.RefreshToken ?? '',
    expiresAt: payload.expiresAt ?? payload.ExpiresAt ?? '',
    refreshTokenExpiresAt: payload.refreshTokenExpiresAt ?? payload.RefreshTokenExpiresAt ?? '',
  }

  storage.value = {
    ...storage.value,
    ...normalized,
  }
}

export function clearTokens() {
  storage.value = {
    accessToken: '',
    refreshToken: '',
    expiresAt: '',
    refreshTokenExpiresAt: '',
  }
}
