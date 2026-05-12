<script setup lang="ts">
import { NButton, NModal } from 'naive-ui'
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'
import Cropper from 'cropperjs'
import 'cropperjs/dist/cropper.css'

const props = defineProps<{
  visible: boolean
  imageUrl: string
  uploading: boolean
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  crop: [blob: Blob]
}>()

const imageRef = ref<HTMLImageElement | null>(null)
let cropper: Cropper | null = null

function initCropper() {
  destroyCropper()
  if (!imageRef.value || !imageRef.value.complete) return

  cropper = new Cropper(imageRef.value, {
    aspectRatio: 1,
    viewMode: 1,
    dragMode: 'move',
    autoCropArea: 0.8,
    cropBoxMovable: true,
    cropBoxResizable: false,
    background: false,
    guides: true,
    center: true,
    highlight: false,
  })
}

function destroyCropper() {
  if (cropper) {
    cropper.destroy()
    cropper = null
  }
}

function handleImageLoad() {
  void nextTick(() => initCropper())
}

function handleConfirm() {
  if (!cropper) return

  const canvas = cropper.getCroppedCanvas({
    width: 256,
    height: 256,
    imageSmoothingEnabled: true,
    imageSmoothingQuality: 'high',
  })

  canvas.toBlob((blob) => {
    if (blob) {
      emit('crop', blob)
    }
  }, 'image/png')
}

function handleClose() {
  emit('update:visible', false)
}

watch(
  () => props.visible,
  (val) => {
    if (val) {
      void nextTick(() => {
        setTimeout(() => initCropper(), 150)
      })
    } else {
      destroyCropper()
    }
  },
)

onBeforeUnmount(() => {
  destroyCropper()
})
</script>

<template>
  <n-modal
    :show="visible"
    :mask-closable="false"
    preset="card"
    style="width: 540px"
    title="裁剪头像"
    @update:show="handleClose"
  >
    <div class="cropper-wrapper">
      <img
        v-if="imageUrl"
        ref="imageRef"
        :src="imageUrl"
        class="cropper-image"
        crossorigin="anonymous"
        @load="handleImageLoad"
      >
    </div>
    <template #footer>
      <div class="cropper-footer">
        <n-button @click="handleClose">取消</n-button>
        <n-button type="primary" :loading="uploading" @click="handleConfirm">
          确认上传
        </n-button>
      </div>
    </template>
  </n-modal>
</template>

<style scoped>
.cropper-wrapper {
  position: relative;
  width: 100%;
  max-height: 420px;
  overflow: hidden;
  background: #f5f5f5;
  border-radius: 4px;
}

.cropper-image {
  display: block;
  max-width: 100%;
  height: auto;
}

.cropper-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>

<style>
/* Make the crop box circular for avatar crop */
.cropper-wrapper .cropper-view-box,
.cropper-wrapper .cropper-face {
  border-radius: 50%;
}
</style>
