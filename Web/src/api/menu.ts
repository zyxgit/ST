import request from '@/lib/request'
import type { IdResult } from '@/types/common'
import type { MenuCommand, MenuDetail, MenuTreeNode } from '@/types/menu'

export function getMenuTree() {
  return request.get<MenuTreeNode[]>('/identity/api/menus/tree')
}

export function getCurrentUserMenuTree() {
  return request.get<MenuTreeNode[]>('/identity/api/menus/my-tree')
}

export function getMenuDetail(id: string) {
  return request.get<MenuDetail>(`/identity/api/menus/${id}`)
}

export function createMenu(data: MenuCommand) {
  return request.post<IdResult>('/identity/api/menus', data)
}

export function updateMenu(id: string, data: MenuCommand) {
  return request.put<void>(`/identity/api/menus/${id}`, data)
}

export function deleteMenu(id: string) {
  return request.delete<void>(`/identity/api/menus/${id}`)
}
