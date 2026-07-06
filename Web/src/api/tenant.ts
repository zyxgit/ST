import request from '@/lib/request'
import type { IdResult, PagedResult } from '@/types/common'
import type {
  AddTenantUserCommand,
  CreateTenantCommand,
  TenantDetail,
  TenantListItem,
  TenantQuota,
  TenantQuery,
  TenantUser,
  UpdateTenantCommand,
  UpdateTenantQuotaCommand,
} from '@/types/tenant'

export function getTenants(params: TenantQuery) {
  return request.get<PagedResult<TenantListItem>>('/identity/tenants', { params })
}

export function getTenantDetail(id: string) {
  return request.get<TenantDetail>(`/identity/tenants/${id}`)
}

export function createTenant(data: CreateTenantCommand) {
  return request.post<IdResult>('/identity/tenants', data)
}

export function updateTenant(id: string, data: UpdateTenantCommand) {
  return request.put<void>(`/identity/tenants/${id}`, data)
}

export function activateTenant(id: string) {
  return request.post<void>(`/identity/tenants/${id}/activate`)
}

export function suspendTenant(id: string) {
  return request.post<void>(`/identity/tenants/${id}/suspend`)
}

export function deleteTenant(id: string) {
  return request.delete<void>(`/identity/tenants/${id}`)
}

export function getTenantUsers(tenantId: string) {
  return request.get<TenantUser[]>(`/identity/tenants/${tenantId}/users`)
}

export function addTenantUser(tenantId: string, data: AddTenantUserCommand) {
  return request.post<void>(`/identity/tenants/${tenantId}/users`, data)
}

export function removeTenantUser(tenantId: string, userId: string) {
  return request.delete<void>(`/identity/tenants/${tenantId}/users/${userId}`)
}

export function getTenantQuota(tenantId: string) {
  return request.get<TenantQuota>(`/identity/tenants/${tenantId}/quota`)
}

export function updateTenantQuota(tenantId: string, data: UpdateTenantQuotaCommand) {
  return request.put<void>(`/identity/tenants/${tenantId}/quota`, data)
}
