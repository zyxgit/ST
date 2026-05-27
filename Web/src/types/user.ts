import type { PagedRequest } from './common'

export interface RoleOption {
  id: string
  code?: string
  name: string
}

export interface UserQuery extends PagedRequest {
  keyword?: string
  isEnable?: boolean | null
  roleId?: string | null
}

export interface UserListItem {
  id: string
  nickName: string
  email: string
  phone: string
  isEnable: boolean
  createTime: string
  modifyTime: string
  lastLoginTime?: string | null
  lastLoginIp?: string | null
  avatarFileId?: string | null
  roles: string[]
}

export interface UserDetail {
  id: string
  nickName: string
  email: string
  phone: string
  isEnable: boolean
  createTime: string
  modifyTime: string
  lastLoginTime?: string | null
  lastLoginIp?: string | null
  avatarFileId?: string | null
  roles: RoleOption[]
}

export interface SetUserAvatarCommand {
  avatarFileId?: string | null
}

export interface CreateUserCommand {
  nickName: string
  email: string
  phone?: string | null
  password: string
  isEnable: boolean
  roleIds: string[]
}

export interface UpdateUserCommand {
  nickName: string
  email: string
  phone?: string | null
  isEnable: boolean
  roleIds: string[]
}

export interface SendEmailCodeCommand {
  email: string
  codePurpose: number
}

export interface ChangeEmailCommand {
  newEmail: string
  currentEmailVerifyCode: string
  newEmailVerifyCode: string
}

export interface EmailExistsResult {
  exists: boolean
}

export interface ChangeUserStatusCommand {
  isEnable: boolean
}

export interface ChangePasswordCommand {
  oldPassword: string
  newPassword: string
}

export interface ResetUserPasswordCommand {
  password: string
}
