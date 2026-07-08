import request from '@/lib/request'
import type { PaymentDto } from '@/types/payment'

/** 查询支付记录 */
export function getPayment(orderId: string) {
  return request.get<PaymentDto>(`/payments/${orderId}`)
}

/** 模拟支付成功 */
export function mockPay(orderId: string) {
  return request.post<PaymentDto>('/payments/mock/pay', null, {
    params: { orderId },
  })
}

/** 模拟支付失败 */
export function mockFail(orderId: string, reason = '模拟支付失败') {
  return request.post<PaymentDto>('/payments/mock/fail', null, {
    params: { orderId, reason },
  })
}
