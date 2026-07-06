import request from '@/lib/request'
import type { IdResult } from '@/types/common'
import type { MenuCommand, MenuDetail, MenuTreeNode } from '@/types/menu'

export function getMenuTree() {
  return request.get<MenuTreeNode[]>('/identity/menus/tree')
}

export function getCurrentUserMenuTree() {
  return request.get<MenuTreeNode[]>('/identity/menus/my-tree')
}

export function getMenuDetail(id: string) {
  return request.get<MenuDetail>(`/identity/menus/${id}`)
}

export function createMenu(data: MenuCommand) {
  return request.post<IdResult>('/identity/menus', data)
}

export function updateMenu(id: string, data: MenuCommand) {
  return request.put<void>(`/identity/menus/${id}`, data)
}

export function deleteMenu(id: string) {
  return request.delete<void>(`/identity/menus/${id}`)
}
