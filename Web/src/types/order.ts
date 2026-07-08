import type { PagedRequest } from './common'

export interface OrderItemDto {
  skuId: string
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
}

export interface OrderDto {
  id: string
  orderNo: string
  userId: string
  totalAmount: number
  status: number
  items: OrderItemDto[]
  createTime: string
  cancelReason?: string | null
}

export interface CreateOrderItemDto {
  skuId: string
  productName: string
  quantity: number
  unitPrice: number
}

export interface CreateOrderDto {
  userId: string
  items: CreateOrderItemDto[]
}

export interface OrderQuery extends PagedRequest {
  orderNo?: string
  status?: number | null
}

/** 订单状态映射 */
export const OrderStatusMap: Record<number, { label: string; type: 'warning' | 'info' | 'success' | 'error' | 'default' }> = {
  0: { label: '待处理', type: 'warning' },
  1: { label: '库存已冻结', type: 'info' },
  2: { label: '已支付', type: 'success' },
  3: { label: '已取消', type: 'default' },
  4: { label: '失败', type: 'error' },
}
