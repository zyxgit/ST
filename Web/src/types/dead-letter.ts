import type { PagedRequest } from './common'

export interface DeadLetterQuery extends PagedRequest {
  queueName?: string
  isReplayed?: boolean | null
  startTime?: string | null
  endTime?: string | null
}

export interface DeadLetterListItem {
  id: string
  queueName: string
  exchangeName: string
  routingKey: string
  errorMessage?: string | null
  retryCount: number
  maxRetryCount: number
  messageCreatedAtUtc?: string | null
  createdAtUtc: string
  isReplayed: boolean
  replayedAtUtc?: string | null
  replayResult?: string | null
}

export interface DeadLetterDetail extends DeadLetterListItem {
  originalMessage: string
  errorStackTrace?: string | null
}

export interface BatchReplayRequest {
  ids: string[]
}

export interface BatchReplayResult {
  replayed: number
  failed: number
}
