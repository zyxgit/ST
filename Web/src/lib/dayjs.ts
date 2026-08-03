import dayjs from 'dayjs'
import utc from 'dayjs/plugin/utc'
import timezone from 'dayjs/plugin/timezone'

dayjs.extend(utc)
dayjs.extend(timezone)

/**
 * 格式化日期时间。
 * 后端返回 UTC 时间，前端自动转换为本地时间显示。
 * 支持格式：ISO 8601（带/不带 Z）、时间戳、Date 对象。
 */
export function formatDateTime(value?: string | number | Date | null, format = 'YYYY-MM-DD HH:mm:ss') {
  if (!value) {
    return '-'
  }

  // 字符串类型：判断是否为 UTC 时间
  if (typeof value === 'string') {
    // 带 Z 后缀或带 +00:00 的明确 UTC 时间
    if (value.endsWith('Z') || value.endsWith('+00:00')) {
      return dayjs.utc(value).local().format(format)
    }
    // 不带时区信息的 ISO 字符串（如 "2024-01-01T00:00:00"），后端通常返回 UTC
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$/.test(value)) {
      return dayjs.utc(value).local().format(format)
    }
    // 带时区偏移的字符串（如 "+08:00"），dayjs 会自动处理
    return dayjs(value).format(format)
  }

  // 数字（时间戳）或 Date 对象，dayjs 自动处理
  return dayjs(value).format(format)
}
