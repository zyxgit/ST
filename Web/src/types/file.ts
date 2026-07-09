import type { PagedRequest } from './common'

export interface FileQuery extends PagedRequest {
  keyword?: string
  accessLevel?: number | null
  contentType?: string
}

export interface FileListItem {
  id: string
  fileName: string
  fileSize: number
  contentType: string
  extension: string
  accessLevel: number
  uploaderName?: string | null
  createTime: string
  url: string
}
