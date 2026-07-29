import request from '@/lib/request'
import type { CreateSkuDto, SkuDto } from '@/types/inventory'

/** SKU 列表 */
export function getSkus() {
  return request.get<SkuDto[]>('/inventory/skus')
}

/** 创建 SKU */
export function createSku(data: CreateSkuDto) {
  return request.post<SkuDto>('/inventory/skus', data)
}

/** 查询单个 SKU 库存 */
export function getStock(skuId: string) {
  return request.get<SkuDto>(`/inventory/skus/${skuId}/stock`)
}

/** 增加库存 */
export function increaseStock(skuId: string, quantity: number) {
  return request.post<SkuDto>(`/inventory/skus/${skuId}/stock/increase`, null, {
    params: { quantity },
  })
}

/** 扣减库存 */
export function deductStock(skuId: string, quantity: number) {
  return request.post<SkuDto>(`/inventory/skus/${skuId}/stock/deduct`, null, {
    params: { quantity },
  })
}
