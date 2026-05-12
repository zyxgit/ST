import type { MenuOption } from 'naive-ui'

import type { MenuTreeNode } from '@/types/menu'

export interface AdminSection {
  key: string
  title: string
  path: string
  code: string
  iconName?: string | null
  children?: MenuTreeNode[]
}

function isVisibleMenu(menu: MenuTreeNode) {
  return !menu.isHide && menu.type !== 3
}

function mapTreeToMenuOptions(items: MenuTreeNode[]): MenuOption[] {
  return items.filter(isVisibleMenu).map((item) => {
    const children = mapTreeToMenuOptions(item.children ?? [])
    return {
      key: normalizePath(item.path),
      label: item.name,
      iconName: item.menuIcon,
      ...(children.length ? { children } : {}),
    }
  })
}

export function normalizePath(path?: string | null) {
  if (!path) {
    return '/dashboard'
  }

  return path.startsWith('/') ? path : `/${path}`
}

export function buildTopSections(menuTree: MenuTreeNode[]): AdminSection[] {
  return [
    { key: '/dashboard', title: '工作台', path: '/dashboard', code: 'dashboard' },
    ...menuTree.filter(isVisibleMenu).map((menu) => ({
      key: normalizePath(menu.path),
      title: menu.name,
      path: normalizePath(menu.path),
      code: menu.code,
      iconName: menu.menuIcon,
      children: menu.children.filter(isVisibleMenu),
    })),
  ]
}

export function buildSidebarOptions(activeTopKey: string, menuTree: MenuTreeNode[]): MenuOption[] {
  const sections = buildTopSections(menuTree)
  const activeSection = sections.find((item) => item.key === activeTopKey) ?? sections[0]

  if (!activeSection) {
    return []
  }

  if (activeSection.key === '/dashboard') {
    return [{ key: '/dashboard', label: '工作台' }]
  }

  const children = activeSection.children ?? []
  if (!children.length) {
    return [{ key: activeSection.path, label: activeSection.title }]
  }

  return children.map((item) => ({
    key: normalizePath(item.path),
    label: item.name,
    iconName: item.menuIcon,
  }))
}

export function buildSideMenuOptions(menuTree: MenuTreeNode[]): MenuOption[] {
  return [
    { key: '/dashboard', label: '工作台' },
    ...mapTreeToMenuOptions(menuTree),
  ]
}

export function findTopMenuKeyByPath(path: string, menuTree: MenuTreeNode[]) {
  const sections = buildTopSections(menuTree)
  const matched = sections.find((item) => path === item.path || path.startsWith(`${item.path}/`))
  if (matched) {
    return matched.key
  }

  for (const section of sections) {
    if (section.children?.some((child) => normalizePath(child.path) === path)) {
      return section.key
    }
  }

  return '/dashboard'
}

export function getActiveSection(activeTopKey: string, menuTree: MenuTreeNode[]) {
  return buildTopSections(menuTree).find((item) => item.key === activeTopKey) ?? null
}

export function buildBreadcrumbs(path: string, menuTree: MenuTreeNode[]) {
  const topKey = findTopMenuKeyByPath(path, menuTree)
  const topSection = getActiveSection(topKey, menuTree)

  if (!topSection) {
    return ['管理后台']
  }

  if (topSection.key === '/dashboard') {
    return ['工作台']
  }

  const matchedChild = topSection.children?.find((item) => {
    const childPath = normalizePath(item.path)
    return path === childPath || path.startsWith(`${childPath}/`)
  })

  return matchedChild ? [topSection.title, matchedChild.name] : [topSection.title]
}
