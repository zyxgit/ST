<script setup lang="ts">
import { NButton, NResult } from 'naive-ui'
import { useRouter } from 'vue-router'

const router = useRouter()
</script>

<template>
  <div class="not-found">
    <n-result description="当前页面不存在或路由尚未配置完成" status="404" title="页面走丢了">
      <template #footer>
        <n-button type="primary" @click="router.push('/dashboard')">返回工作台</n-button>
      </template>
    </n-result>
  </div>
</template>

<style scoped>
.not-found {
  display: grid;
  min-height: 100vh;
  place-items: center;
}
</style>
