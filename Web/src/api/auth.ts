import request from '@/lib/request'
import type { CurrentUser, LoginCommand, LoginResult, RefreshTokenCommand } from '@/types/auth'

export function login(data: LoginCommand) {
  return request.post<LoginResult>('/identity/user/login', data)
}

export function logout(data: RefreshTokenCommand) {
  return request.post<void>('/identity/user/logout', data)
}

export function getCurrentUser() {
  return request.get<CurrentUser>('/identity/user/me')
}
