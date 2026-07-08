export interface SkuDto {
  skuId: string
  productName: string
  available: number
  frozen: number
  sold: number
  totalStock: number
}

export interface CreateSkuDto {
  skuId: string
  productName: string
  initialStock: number
}
