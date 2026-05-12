import request from '@/lib/request'

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
