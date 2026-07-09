export interface PaymentDto {
  id: string
  orderId: string
  amount: number
  status: string
  failureReason?: string | null
  createTime: string
}
