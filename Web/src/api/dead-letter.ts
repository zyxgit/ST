import request from '@/lib/request'
import type { PagedResult } from '@/types/common'
import type {
  BatchReplayRequest,
  BatchReplayResult,
  DeadLetterDetail,
  DeadLetterListItem,
  DeadLetterQuery,
} from '@/types/dead-letter'

export function getDeadLetters(params: DeadLetterQuery) {
  return request.get<PagedResult<DeadLetterListItem>>('/operationlog/dead-letters', { params })
}

export function getDeadLetterDetail(id: string) {
  return request.get<DeadLetterDetail>(`/operationlog/dead-letters/${id}`)
}

export function replayDeadLetter(id: string) {
  return request.post<{ success: boolean }>(`/operationlog/dead-letters/${id}/replay`)
}

export function batchReplayDeadLetters(ids: string[]) {
  return request.post<BatchReplayResult>('/operationlog/dead-letters/batch-replay', { ids } satisfies BatchReplayRequest)
}
