import {
  AppsOutline,
  BookOutline,
  CardOutline,
  DocumentTextOutline,
  FlashOutline,
  FolderOpenOutline,
  GridOutline,
  HomeOutline,
  KeyOutline,
  ListOutline,
  PeopleOutline,
  PersonOutline,
  ReceiptOutline,
  SettingsOutline,
  ShieldOutline,
} from '@vicons/ionicons5'
import type { Component } from 'vue'

export const menuIconMap: Record<string, Component> = {
  'apps-outline': AppsOutline,
  'book-outline': BookOutline,
  'card-outline': CardOutline,
  'document-text-outline': DocumentTextOutline,
  'flash-outline': FlashOutline,
  'folder-open-outline': FolderOpenOutline,
  'grid-outline': GridOutline,
  'home-outline': HomeOutline,
  'key-outline': KeyOutline,
  'list-outline': ListOutline,
  'people-outline': PeopleOutline,
  'person-outline': PersonOutline,
  'receipt-outline': ReceiptOutline,
  'settings-outline': SettingsOutline,
  'shield-outline': ShieldOutline,
}

export const menuIconOptions = [
  { label: '首页', value: 'home-outline' },
  { label: '设置', value: 'settings-outline' },
  { label: '用户组', value: 'people-outline' },
  { label: '用户', value: 'person-outline' },
  { label: '角色权限', value: 'shield-outline' },
  { label: '列表菜单', value: 'list-outline' },
  { label: '日志', value: 'receipt-outline' },
  { label: '文档', value: 'document-text-outline' },
  { label: '目录', value: 'folder-open-outline' },
  { label: '应用', value: 'apps-outline' },
  { label: '宫格', value: 'grid-outline' },
  { label: '书籍', value: 'book-outline' },
  { label: '钥匙', value: 'key-outline' },
  { label: '闪电', value: 'flash-outline' },
  { label: '卡片', value: 'card-outline' },
]

export function resolveMenuIcon(iconName?: string | null) {
  return (iconName ? menuIconMap[iconName] : undefined) ?? null
}
