<script setup lang="ts">
defineProps<{
  title: string
  description?: string
}>()
</script>

<template>
  <div class="page-shell">
    <div>
      <h1 class="page-title">{{ title }}</h1>
      <p v-if="description" class="page-subtitle">{{ description }}</p>
    </div>
    <slot />
  </div>
</template>
