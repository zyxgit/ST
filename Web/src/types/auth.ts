export interface LoginCommand {
  email: string
  password: string
}

export interface RefreshTokenCommand {
  refreshToken: string
}

export interface LoginResult {
  accessToken: string
  expiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

export interface CurrentUser {
  isAuthenticated: boolean
  userId?: string
  email?: string
  nickName?: string
  avatarFileId?: string | null
  roles: string[]
  permissions: string[]
  clientIp?: string
}
