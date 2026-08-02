import type { FormItemRule } from 'naive-ui'

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const phonePattern = /^1\d{10}$/
const codePattern = /^[A-Za-z][A-Za-z0-9:_-]*$/

export function requiredRule(label: string): FormItemRule {
  return {
    required: true,
    message: `请输入${label}`,
    trigger: ['input', 'blur', 'change'],
    validator: (_rule, value: unknown) => {
      if (typeof value === 'string') {
        return value.trim().length > 0
      }

      return value !== null && value !== undefined && value !== ''
    },
  }
}

export function emailRule(label = '邮箱'): FormItemRule {
  return {
    trigger: ['input', 'blur'],
    validator: (_rule, value: unknown) => {
      if (typeof value !== 'string' || !value.trim()) {
        return new Error(`请输入${label}`)
      }

      if (!emailPattern.test(value.trim())) {
        return new Error(`请输入正确的${label}`)
      }

      return true
    },
  }
}

export function optionalPhoneRule(label = '手机号'): FormItemRule {
  return {
    trigger: ['input', 'blur'],
    validator: (_rule, value: unknown) => {
      if (value === null || value === undefined || value === '') {
        return true
      }

      if (typeof value !== 'string' || !phonePattern.test(value.trim())) {
        return new Error(`请输入正确的${label}`)
      }

      return true
    },
  }
}

export function requiredPhoneRule(label = '手机号'): FormItemRule {
  return {
    trigger: ['input', 'blur'],
    validator: (_rule, value: unknown) => {
      if (value === null || value === undefined || value === '') {
        return new Error(`请输入${label}`)
      }

      if (typeof value !== 'string' || !phonePattern.test(value.trim())) {
        return new Error(`请输入正确的${label}`)
      }

      return true
    },
  }
}

export function passwordRule(label = '密码', min = 6, max = 32): FormItemRule {
  return {
    trigger: ['input', 'blur'],
    validator: (_rule, value: unknown) => {
      if (typeof value !== 'string' || !value.trim()) {
        return new Error(`请输入${label}`)
      }

      const length = value.trim().length
      if (length < min || length > max) {
        return new Error(`${label}长度需在 ${min} 到 ${max} 位之间`)
      }

      return true
    },
  }
}

export function arrayRequiredRule(label: string): FormItemRule {
  return {
    type: 'array',
    required: true,
    trigger: ['change', 'blur'],
    validator: (_rule, value: unknown) => {
      if (!Array.isArray(value) || value.length === 0) {
        return new Error(`请至少选择一个${label}`)
      }

      return true
    },
  }
}

export function codeRule(label = '编码'): FormItemRule {
  return {
    trigger: ['input', 'blur'],
    validator: (_rule, value: unknown) => {
      if (typeof value !== 'string' || !value.trim()) {
        return new Error(`请输入${label}`)
      }

      if (!codePattern.test(value.trim())) {
        return new Error(`${label}需以字母开头，可包含字母、数字、冒号、下划线或短横线`)
      }

      return true
    },
  }
}

export function pathRule(label = '路由路径'): FormItemRule {
  return {
    trigger: ['input', 'blur'],
    validator: (_rule, value: unknown) => {
      if (typeof value !== 'string' || !value.trim()) {
        return new Error(`请输入${label}`)
      }

      if (!value.trim().startsWith('/')) {
        return new Error(`${label}需以 / 开头`)
      }

      return true
    },
  }
}
