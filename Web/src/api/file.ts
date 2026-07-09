import request from '@/lib/request'
import type { PagedResult } from '@/types/common'
import type { FileQuery, FileListItem } from '@/types/file'

export interface FileUploadResult {
  id: string
  fileName: string
  fileSize: number
  contentType: string
  url: string
  uploaderName?: string | null
}

/**
 * 上传文件
 * @param file 表单文件
 * @param accessLevel 访问级别（Public=0, Private=1），头像请传 0
 */
export function uploadFile(file: File, accessLevel = 0) {
  const formData = new FormData()
  formData.append('file', file)
  if (accessLevel !== undefined) {
    formData.append('accessLevel', String(accessLevel))
  }
  return request.post<FileUploadResult>('/files/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

/** 文件列表分页查询 */
export function getFiles(params: FileQuery) {
  return request.get<PagedResult<FileListItem>>('/files', { params })
}

/** 删除文件（仅上传者可删除） */
export function deleteFile(id: string) {
  return request.delete<void>(`/files/${id}`)
}
