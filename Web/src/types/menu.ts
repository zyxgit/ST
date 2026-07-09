export interface MenuTreeNode {
  id: string
  parentId?: string | null
  code: string
  name: string
  type: PermissionType
  path?: string | null
  menuIcon?: string | null
  component?: string | null
  isLink: boolean
  keepAlive: boolean
  isHide: boolean
  sort: number
  children: MenuTreeNode[]
}

export interface MenuDetail extends MenuTreeNode {
  createTime: string
  modifyTime: string
}

export interface MenuCommand {
  parentId?: string | null
  code: string
  name: string
  type: PermissionType
  path?: string | null
  menuIcon?: string | null
  component?: string | null
  isLink: boolean
  keepAlive: boolean
  isHide: boolean
  sort: number
}

export enum PermissionType {
  Catalogue = 1,
  Menu = 2,
  Button = 3,
}
