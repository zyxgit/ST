<script setup lang="ts">
import { useAppStore } from '@/stores/app'

const appStore = useAppStore()
</script>

<template>
  <div class="app-logo" :class="{ 'app-logo--collapsed': appStore.collapsed }">
    <div class="app-logo__badge">ST</div>
    <div v-if="!appStore.collapsed" class="app-logo__text">
      <strong>ST Admin</strong>
      <span>Service Template Console</span>
    </div>
  </div>
</template>

<style scoped>
.app-logo {
  display: flex;
  align-items: center;
  gap: 12px;
  height: 64px;
  padding: 0 18px;
  border-bottom: 1px solid var(--panel-border);
}

.app-logo--collapsed {
  justify-content: center;
  padding: 0;
}

.app-logo__badge {
  display: grid;
  width: 38px;
  height: 38px;
  place-items: center;
  border-radius: 12px;
  background: linear-gradient(135deg, #2563eb 0%, #0f766e 100%);
  color: #fff;
  font-size: 15px;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.app-logo__text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.app-logo__text strong {
  color: var(--text-1);
  font-size: 15px;
}

.app-logo__text span {
  color: var(--text-3);
  font-size: 12px;
}
</style>
