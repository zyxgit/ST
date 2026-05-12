import request from '@/lib/request'
import type { PagedResult } from '@/types/common'
import type { OperationLogDetail, OperationLogListItem, OperationLogQuery } from '@/types/operation-log'

export function getOperationLogs(params: OperationLogQuery) {
  return request.get<PagedResult<OperationLogListItem>>('/operationlog/api/operation-logs', { params })
}

export function getOperationLogDetail(id: number) {
  return request.get<OperationLogDetail>(`/operationlog/api/operation-logs/${id}`)
}
