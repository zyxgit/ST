import request from '@/lib/request'
import type { IdResult, PagedResult } from '@/types/common'
import type {
  ChangeEmailCommand,
  ChangePasswordCommand,
  ChangeUserStatusCommand,
  CreateUserCommand,
  EmailExistsResult,
  ResetUserPasswordCommand,
  RoleOption,
  SendEmailCodeCommand,
  SetUserAvatarCommand,
  UpdateUserCommand,
  UserDetail,
  UserListItem,
  UserQuery,
} from '@/types/user'

export function getRoleOptions() {
  return request.get<RoleOption[]>('/identity/api/user/roles/options')
}

export function getUsers(params: UserQuery) {
  return request.get<PagedResult<UserListItem>>('/identity/api/user/users', { params })
}

export function getUserDetail(id: string) {
  return request.get<UserDetail>(`/identity/api/user/users/${id}`)
}

export function createUser(data: CreateUserCommand) {
  return request.post<IdResult>('/identity/api/user/users', data)
}

export function updateUser(id: string, data: UpdateUserCommand) {
  return request.put<void>(`/identity/api/user/users/${id}`, data)
}

export function checkEmailExists(email: string, excludeUserId?: string) {
  return request.get<EmailExistsResult>('/identity/api/user/users/email-exists', {
    params: {
      email,
      excludeUserId,
    },
  })
}

export function sendEmailCode(data: SendEmailCodeCommand) {
  return request.post<void>('/identity/api/user/email', data)
}

export function changeMyEmail(data: ChangeEmailCommand) {
  return request.put<void>('/identity/api/user/me/email', data)
}

export function changeMyPassword(data: ChangePasswordCommand) {
  return request.put<void>('/identity/api/user/me/password', data)
}

export function changeUserStatus(id: string, data: ChangeUserStatusCommand) {
  return request.put<void>(`/identity/api/user/users/${id}/status`, data)
}

export function resetUserPassword(id: string, data: ResetUserPasswordCommand) {
  return request.put<void>(`/identity/api/user/users/${id}/password/reset`, data)
}

export function deleteUser(id: string) {
  return request.delete<void>(`/identity/api/user/users/${id}`)
}

export function setUserAvatar(id: string, data: SetUserAvatarCommand) {
  return request.put<void>(`/identity/api/user/users/${id}/avatar`, data)
}

export function deleteUserAvatar(id: string) {
  return request.delete<void>(`/identity/api/user/users/${id}/avatar`)
}
