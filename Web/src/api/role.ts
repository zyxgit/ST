import request from '@/lib/request'
import type { IdResult, PagedResult } from '@/types/common'
import type { RoleCommand, RoleDetail, RoleListItem, RoleQuery } from '@/types/role'

export function getRoles(params: RoleQuery) {
  return request.get<PagedResult<RoleListItem>>('/identity/api/roles', { params })
}

export function getRoleDetail(id: string) {
  return request.get<RoleDetail>(`/identity/api/roles/${id}`)
}

export function createRole(data: RoleCommand) {
  return request.post<IdResult>('/identity/api/roles', data)
}

export function updateRole(id: string, data: RoleCommand) {
  return request.put<void>(`/identity/api/roles/${id}`, data)
}

export function deleteRole(id: string) {
  return request.delete<void>(`/identity/api/roles/${id}`)
}
