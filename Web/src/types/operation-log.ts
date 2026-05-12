import type { PagedRequest } from './common'

export interface OperationLogQuery extends PagedRequest {
  serviceName?: string
  userId?: string | null
  traceId?: string
  method?: string
  path?: string
  operationName?: string
  success?: boolean | null
  statusCode?: number | null
  keyword?: string
  startTimeUtc?: string | null
  endTimeUtc?: string | null
}

export interface OperationLogListItem {
  id: number
  createdAtUtc: string
  serviceName: string
  userId?: string | null
  userName?: string | null
  operationName: string
  path: string
  method: string
  ip: string
  statusCode: number
  success: boolean
  durationMs: number
  traceId: string
  exceptionMessage?: string | null
}

export interface OperationLogDetail extends OperationLogListItem {
  spanId?: string | null
  requestJson?: string | null
  responseJson?: string | null
  exceptionType?: string | null
  exceptionStackTrace?: string | null
  tagsJson?: string | null
  extraJson?: string | null
}
