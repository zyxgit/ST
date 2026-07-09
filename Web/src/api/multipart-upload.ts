import request from '@/lib/request'
import { AxiosError } from 'axios'

/** 最大重试次数 */
const MAX_RETRIES = 3
/** 基础退避时间 (ms) */
const BASE_BACKOFF = 1000

/**
 * 带重试的分片上传。
 * 遇到 429 时解析 Retry-After 头等待后重试，否则指数退避。
 * @param onRetry 重试回调，用于 UI 显示等待信息
 */
async function uploadChunkWithRetry(
  uploadId: string,
  chunkIndex: number,
  file: Blob,
  chunkHash?: string,
  onRetry?: (attempt: number, waitSeconds: number) => void,
) {
  for (let attempt = 0; attempt <= MAX_RETRIES; attempt++) {
    try {
      return await uploadChunkRaw(uploadId, chunkIndex, file, chunkHash)
    } catch (error) {
      const axiosErr = error as AxiosError
      const is429 = axiosErr.response?.status === 429

      if (!is429 || attempt >= MAX_RETRIES) {
        throw error
      }

      // 解析 Retry-After 头，否则指数退避
      const retryAfterHeader = axiosErr.response?.headers?.['retry-after']
      const waitSeconds = retryAfterHeader
        ? Math.ceil(parseFloat(retryAfterHeader))
        : Math.ceil(BASE_BACKOFF * 2 ** attempt / 1000)

      onRetry?.(attempt + 1, waitSeconds)
      await new Promise((r) => setTimeout(r, waitSeconds * 1000))
    }
  }
  throw new Error('Max retries exceeded')
}

export interface InitUploadRequest {
  fileName: string
  fileSize: number
  chunkSize?: number
  fileHash?: string
  contentType?: string
  accessLevel?: number
}

export interface InitUploadResult {
  uploadId: string
  fileName: string
  fileSize: number
  chunkSize: number
  totalChunks: number
  status: string
  expiresAtUtc: string
}

export interface UploadStatus {
  uploadId: string
  fileName: string
  fileSize: number
  totalChunks: number
  uploadedChunks: number
  uploadedChunkIndexes: number[]
  missingChunkIndexes: number[]
  status: string
  progress: number
  fileId?: string
}

export interface CheckByHashResult {
  exists: boolean
  fileId?: string
  fileName?: string
  fileSize?: number
}

/** 初始化分片上传 */
export function initMultipartUpload(data: InitUploadRequest) {
  return request.post<InitUploadResult>('/files/multipart/init', data)
}

/** 查询上传状态 */
export function getUploadStatus(uploadId: string) {
  return request.get<UploadStatus>(`/files/multipart/${uploadId}/status`)
}

/** 上传单个分片（原始请求，429 时静默不弹 dialog） */
function uploadChunkRaw(uploadId: string, chunkIndex: number, file: Blob, chunkHash?: string) {
  const formData = new FormData()
  formData.append('file', file)
  if (chunkHash) {
    formData.append('chunkHash', chunkHash)
  }
  return request.post(`/files/multipart/${uploadId}/chunks/${chunkIndex}`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    silent: true,
  } as any)
}

/**
 * 上传单个分片（带 429 自动重试）。
 * 遇到限流时解析 Retry-After 头等待后重试，否则指数退避。
 */
export const uploadChunk = uploadChunkWithRetry

/** 完成上传 */
export function completeUpload(uploadId: string) {
  return request.post(`/files/multipart/${uploadId}/complete`)
}

/** 秒传检查 */
export function checkByHash(fileHash: string, fileSize: number) {
  return request.post<CheckByHashResult>('/files/multipart/check-by-hash', { fileHash, fileSize })
}

/** 取消上传 */
export function cancelUpload(uploadId: string) {
  return request.delete(`/files/multipart/${uploadId}`)
}
