import request from '@/lib/request'
import type { PagedResult } from '@/types/common'
import type { CreateOrderDto, OrderDto, OrderQuery } from '@/types/order'

/** 创建订单 */
export function createOrder(data: CreateOrderDto) {
  return request.post<OrderDto>('/orders', data)
}

/** 订单列表 */
export function getOrders(params: OrderQuery) {
  return request.get<PagedResult<OrderDto>>('/orders', { params })
}

/** 订单详情 */
export function getOrder(id: string) {
  return request.get<OrderDto>(`/orders/${id}`)
}

/** 取消订单 */
export function cancelOrder(id: string, reason = '用户取消') {
  return request.post<OrderDto>(`/orders/${id}/cancel`, { reason })
}
