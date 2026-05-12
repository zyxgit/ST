# Composables（Hooks）规范

## 目录

- [事实](#事实)
- [Naive 离散 API](#naive-离散-api)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 事实

- 项目使用 `Web/src/lib/naive.ts` 的 **`useDiscrete()`** 在非 setup 上下文外获得 `message`/`dialog` 等（如 Axios 拦截器）。

## Naive 离散 API

```typescript
export function useDiscrete() {
  if (!message || !dialog || !notification || !loadingBar) {
    setupNaiveDiscreteApi()
  }
  return {
    message: message!,
    dialog: dialog!,
    notification: notification!,
    loadingBar: loadingBar!,
  }
}
```

`main.ts` 应调用一次 **`setupNaiveDiscreteApi()`**（若尚未，与现有启动顺序对齐）。

## 代码示例

在 `setup` 中：

```typescript
import { useDiscrete } from '@/lib/naive'

const { message } = useDiscrete()
message.success('保存成功')
```

## 推荐方案

- 可复用逻辑：`composables/useXxx.ts`（若目录尚不存在，可新建并集中放非 UI 的纯逻辑）。
- 与路由相关：用 `useRouter`/`useRoute`；与权限相关：用 `useAuthStore`。

## 禁止事项

- 禁止在 composable 顶层执行副作用（应暴露 `init` 函数或在 `setup` 内调用）。

## AI 注意事项

- 命名 **`use` 前缀** + camelCase，与 Vue 生态一致。
