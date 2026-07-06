import type { PagedRequest } from './common'

export interface TenantQuery extends PagedRequest {
  keyword?: string
  status?: string | null
}

export interface TenantListItem {
  id: string
  code: string
  name: string
  status: string
  packageId?: string | null
  expireAtUtc?: string | null
  userCount: number
  createTime: string
}

export interface TenantDetail {
  id: string
  code: string
  name: string
  status: string
  packageId?: string | null
  expireAtUtc?: string | null
  userCount: number
  createTime: string
  modifyTime: string
  quota?: TenantQuota | null
}

export interface CreateTenantCommand {
  code: string
  name: string
}

export interface UpdateTenantCommand {
  name: string
  packageId?: string | null
  expireAtUtc?: string | null
}

export interface TenantUser {
  userId: string
  nickName: string
  email: string
  roleInTenant?: string | null
  joinedAtUtc: string
}

export interface AddTenantUserCommand {
  userId: string
  roleInTenant?: string | null
}

export interface TenantQuota {
  tenantId: string
  maxUsers: number
  maxStorageBytes: number
  maxApiCallsPerDay: number
  maxFileSize: number
  maxOrdersPerDay: number
}

export interface UpdateTenantQuotaCommand {
  maxUsers?: number | null
  maxStorageBytes?: number | null
  maxApiCallsPerDay?: number | null
  maxFileSize?: number | null
  maxOrdersPerDay?: number | null
}
