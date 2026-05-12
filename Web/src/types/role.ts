import type { PagedRequest } from './common'

export interface RoleQuery extends PagedRequest {
  keyword?: string
  isSystem?: boolean | null
  isDefault?: boolean | null
}

export interface RoleListItem {
  id: string
  code: string
  name: string
  description: string
  isSystem: boolean
  isDefault: boolean
  userCount: number
  permissionCount: number
  createTime: string
  modifyTime: string
}

export interface RoleDetail {
  id: string
  code: string
  name: string
  description: string
  isSystem: boolean
  isDefault: boolean
  createTime: string
  modifyTime: string
  permissionIds: string[]
}

export interface RoleCommand {
  code: string
  name: string
  description: string
  isSystem: boolean
  isDefault: boolean
  permissionIds: string[]
}
