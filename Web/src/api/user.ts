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
  return request.get<RoleOption[]>('/identity/user/roles/options')
}

export function getUsers(params: UserQuery) {
  return request.get<PagedResult<UserListItem>>('/identity/user/users', { params })
}

export function getUserDetail(id: string) {
  return request.get<UserDetail>(`/identity/user/users/${id}`)
}

export function createUser(data: CreateUserCommand) {
  return request.post<IdResult>('/identity/user/users', data)
}

export function updateUser(id: string, data: UpdateUserCommand) {
  return request.put<void>(`/identity/user/users/${id}`, data)
}

export function checkEmailExists(email: string, excludeUserId?: string) {
  return request.get<EmailExistsResult>('/identity/user/users/email-exists', {
    params: {
      email,
      excludeUserId,
    },
  })
}

export function sendEmailCode(data: SendEmailCodeCommand) {
  return request.post<void>('/identity/user/email', data)
}

export function changeMyEmail(data: ChangeEmailCommand) {
  return request.put<void>('/identity/user/me/email', data)
}

export function changeMyPassword(data: ChangePasswordCommand) {
  return request.put<void>('/identity/user/me/password', data)
}

export function changeUserStatus(id: string, data: ChangeUserStatusCommand) {
  return request.put<void>(`/identity/user/users/${id}/status`, data)
}

export function resetUserPassword(id: string, data: ResetUserPasswordCommand) {
  return request.put<void>(`/identity/user/users/${id}/password/reset`, data)
}

export function deleteUser(id: string) {
  return request.delete<void>(`/identity/user/users/${id}`)
}

export function setUserAvatar(id: string, data: SetUserAvatarCommand) {
  return request.put<void>(`/identity/user/users/${id}/avatar`, data)
}

export function deleteUserAvatar(id: string) {
  return request.delete<void>(`/identity/user/users/${id}/avatar`)
}
