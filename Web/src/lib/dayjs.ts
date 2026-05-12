import dayjs from 'dayjs'

export function formatDateTime(value?: string | number | Date | null, format = 'YYYY-MM-DD HH:mm:ss') {
  if (!value) {
    return '-'
  }

  return dayjs(value).format(format)
}
