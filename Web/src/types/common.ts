export interface PagedRequest {
  pageIndex: number
  pageSize: number
}

export interface PagedResult<T> {
  pageIndex: number
  pageSize: number
  totalCount: number
  items: T[]
}

export interface IdResult<T = string> {
  id: T
}
