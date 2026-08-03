<script setup lang="ts">
import { NButton, NCard, NIcon, NProgress, NSpace, NText } from 'naive-ui'
import { computed, ref } from 'vue'

import PageSection from '@/components/common/PageSection.vue'
import { uploadFile } from '@/api/file'
import { initMultipartUpload, uploadChunk, completeUpload, checkByHash } from '@/api/multipart-upload'

const MAX_FILE_SIZE = 100 * 1024 * 1024   // 100MB
const MAX_CHUNK_SIZE = 500 * 1024 * 1024  // 500MB
const CHUNK_SIZE = 5 * 1024 * 1024        // 5MB

// 图片上传
const imageInputRef = ref<HTMLInputElement | null>(null)
const imagePreview = ref('')
const imageFile = ref<File | null>(null)
const imageUploading = ref(false)
const imageResult = ref('')

// 文件上传
const fileInputRef = ref<HTMLInputElement | null>(null)
const fileFile = ref<File | null>(null)
const fileUploading = ref(false)
const fileResult = ref('')

// 分片上传
const chunkInputRef = ref<HTMLInputElement | null>(null)
const chunkFile = ref<File | null>(null)
const progress = ref(0)
const uploadedChunks = ref(0)
const totalChunks = ref(0)
const currentFileName = ref('')
const uploadStatus = ref<'idle' | 'hashing' | 'uploading' | 'merging' | 'done' | 'error'>('idle')
const uploadResult = ref('')
const retryInfo = ref('')

const progressStatus = computed(() => {
  if (uploadStatus.value === 'done') return 'success'
  if (uploadStatus.value === 'error') return 'error'
  return undefined
})

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return `${(bytes / 1024 ** i).toFixed(1)} ${units[i]}`
}

// === 图片上传 ===
function handleImageSelect(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  if (file.size > MAX_FILE_SIZE) {
    imageResult.value = `文件大小 ${formatFileSize(file.size)} 超过限制，最大 ${formatFileSize(MAX_FILE_SIZE)}`
    return
  }

  imageFile.value = file
  imageResult.value = ''
  const reader = new FileReader()
  reader.onload = (e) => { imagePreview.value = e.target?.result as string }
  reader.readAsDataURL(file)
}

async function handleImageUpload() {
  if (!imageFile.value) return
  imageUploading.value = true
  try {
    const result = await uploadFile(imageFile.value, 0)
    imageResult.value = `上传成功！${result.fileName}`
  } catch (error: any) {
    const detail = error?.response?.data?.detail
    imageResult.value = `上传失败：${detail || (error?.response?.status === 413 ? '文件超过100MB限制' : error?.message || '未知错误')}`
  } finally {
    imageUploading.value = false
  }
}

// === 文件上传 ===
function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  if (file.size > MAX_FILE_SIZE) {
    fileResult.value = `文件大小 ${formatFileSize(file.size)} 超过限制，最大 ${formatFileSize(MAX_FILE_SIZE)}`
    return
  }

  fileFile.value = file
  fileResult.value = ''
}

async function handleFileUpload() {
  if (!fileFile.value) return
  fileUploading.value = true
  try {
    const result = await uploadFile(fileFile.value, 1)
    fileResult.value = `上传成功！${result.fileName}`
  } catch (error: any) {
    const detail = error?.response?.data?.detail
    fileResult.value = `上传失败：${detail || (error?.response?.status === 413 ? '文件超过100MB限制' : error?.message || '未知错误')}`
  } finally {
    fileUploading.value = false
  }
}

/**
 * 计算文件 SHA256 哈希（Web Crypto API，无需第三方库）。
 */
async function computeFileHash(file: File): Promise<string> {
  const hashBuffer = await crypto.subtle.digest('SHA-256', await file.arrayBuffer())
  return Array.from(new Uint8Array(hashBuffer)).map((b) => b.toString(16).padStart(2, '0')).join('')
}

// === 分片上传 ===
function handleChunkSelect(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  if (file.size > MAX_CHUNK_SIZE) {
    uploadResult.value = `文件大小 ${formatFileSize(file.size)} 超过限制，最大 ${formatFileSize(MAX_CHUNK_SIZE)}`
    return
  }

  chunkFile.value = file
  uploadResult.value = ''
  uploadStatus.value = 'idle'
}

async function handleChunkUpload() {
  if (!chunkFile.value) return

  currentFileName.value = chunkFile.value.name
  uploadedChunks.value = 0
  progress.value = 0
  uploadResult.value = ''
  retryInfo.value = ''

  try {
    // 1. 计算文件哈希
    uploadStatus.value = 'hashing'
    const fileHash = await computeFileHash(chunkFile.value)

    // 2. 秒传检查
    const hashCheck = await checkByHash(fileHash, chunkFile.value.size)
    if (hashCheck.exists) {
      uploadStatus.value = 'done'
      uploadResult.value = `秒传成功！文件已存在：${hashCheck.fileName}`
      return
    }

    // 3. 正常分片上传
    uploadStatus.value = 'uploading'

    const initResult = await initMultipartUpload({
      fileName: chunkFile.value.name,
      fileSize: chunkFile.value.size,
      chunkSize: CHUNK_SIZE,
      contentType: chunkFile.value.type,
      accessLevel: 1,
      fileHash,
    })

    totalChunks.value = initResult.totalChunks

    for (let i = 0; i < initResult.totalChunks; i++) {
      const start = i * CHUNK_SIZE
      const end = Math.min(start + CHUNK_SIZE, chunkFile.value.size)
      const chunk = chunkFile.value.slice(start, end)

      await uploadChunk(initResult.uploadId, i, chunk, undefined, (attempt, waitSeconds) => {
        retryInfo.value = `限流中，第 ${attempt} 次重试，等待 ${waitSeconds} 秒...`
      })
      retryInfo.value = ''
      uploadedChunks.value = i + 1
      progress.value = Math.round(((i + 1) / initResult.totalChunks) * 100)
    }

    uploadStatus.value = 'merging'
    await completeUpload(initResult.uploadId)

    uploadStatus.value = 'done'
    uploadResult.value = `上传成功！${chunkFile.value.name}`
  } catch (error: any) {
    uploadStatus.value = 'error'
    retryInfo.value = ''
    const detail = error?.response?.data?.detail
    uploadResult.value = `上传失败：${detail || error?.message || '未知错误'}`
  }
}
</script>

<template>
  <page-section title="文件上传测试" description="测试普通上传和分片上传功能，实时显示上传进度。">
    <n-space vertical :size="16">
      <!-- 图片上传 -->
      <n-card title="图片上传（普通接口，最大100MB）" :bordered="false">
        <n-space vertical :size="16">
          <input ref="imageInputRef" type="file" accept="image/*" style="display: none" @change="handleImageSelect" />
          <n-button @click="imageInputRef?.click()">选择图片</n-button>

          <div v-if="imagePreview" style="margin-top: 8px">
            <img :src="imagePreview" style="max-width: 200px; max-height: 200px; border-radius: 4px; border: 1px solid #eee" />
            <div style="margin-top: 8px">
              <n-text depth="3">{{ imageFile?.name }} ({{ formatFileSize(imageFile?.size || 0) }})</n-text>
            </div>
          </div>

          <n-button type="primary" :loading="imageUploading" :disabled="!imageFile" @click="handleImageUpload">上传图片</n-button>

          <div v-if="imageResult" style="padding: 12px; background: #f5f5f5; border-radius: 4px">
            <n-text>{{ imageResult }}</n-text>
          </div>

          <n-text depth="3" style="font-size: 12px">
            支持格式：JPG、JPEG、PNG、GIF、BMP、WebP、SVG
          </n-text>
        </n-space>
      </n-card>

      <!-- 文件上传 -->
      <n-card title="文件上传（普通接口，最大100MB）" :bordered="false">
        <n-space vertical :size="16">
          <input ref="fileInputRef" type="file" style="display: none" @change="handleFileSelect" />
          <n-button @click="fileInputRef?.click()">选择文件</n-button>

          <div v-if="fileFile" style="padding: 12px; background: #f5f5f5; border-radius: 4px">
            <n-text>{{ fileFile.name }} ({{ formatFileSize(fileFile.size) }})</n-text>
          </div>

          <n-button type="primary" :loading="fileUploading" :disabled="!fileFile" @click="handleFileUpload">上传文件</n-button>

          <div v-if="fileResult" style="padding: 12px; background: #f5f5f5; border-radius: 4px">
            <n-text>{{ fileResult }}</n-text>
          </div>

          <n-text depth="3" style="font-size: 12px">
            支持格式：PDF、DOC、DOCX、XLS、XLSX、PPT、PPTX、TXT、CSV、ZIP、RAR、7Z
          </n-text>
        </n-space>
      </n-card>

      <!-- 分片上传 -->
      <n-card title="分片上传（进度条，最大500MB，文件保留10分钟）" :bordered="false">
        <n-space vertical :size="16">
          <input ref="chunkInputRef" type="file" style="display: none" @change="handleChunkSelect" />
          <n-button @click="chunkInputRef?.click()">选择文件</n-button>

          <div v-if="chunkFile" style="padding: 12px; background: #f5f5f5; border-radius: 4px">
            <n-text>{{ chunkFile.name }} ({{ formatFileSize(chunkFile.size) }})</n-text>
          </div>

          <n-button type="primary" :disabled="!chunkFile || uploadStatus === 'hashing' || uploadStatus === 'uploading' || uploadStatus === 'merging'" @click="handleChunkUpload">
            开始分片上传
          </n-button>

          <template v-if="uploadStatus !== 'idle'">
            <n-progress
              type="line"
              :percentage="progress"
              :status="progressStatus"
              :show-percentage="true"
              :indicator-placement="'inside'"
            />

            <div style="display: flex; justify-content: space-between">
              <n-text depth="3">分片：{{ uploadedChunks }} / {{ totalChunks }}</n-text>
              <n-text v-if="retryInfo" type="warning">{{ retryInfo }}</n-text>
              <n-text v-else-if="uploadStatus === 'hashing'" type="info">计算文件哈希中...</n-text>
              <n-text v-else-if="uploadStatus === 'uploading'" type="info">上传中...</n-text>
              <n-text v-else-if="uploadStatus === 'merging'" type="warning">合并中...</n-text>
              <n-text v-else-if="uploadStatus === 'done'" type="success">完成</n-text>
              <n-text v-else-if="uploadStatus === 'error'" type="error">失败</n-text>
            </div>
          </template>

          <div v-if="uploadResult" style="padding: 12px; background: #f5f5f5; border-radius: 4px">
            <n-text :type="uploadStatus === 'done' ? 'success' : 'error'">
              {{ uploadResult }}
            </n-text>
          </div>

          <n-text depth="3" style="font-size: 12px">
            支持格式：所有文件类型（图片、文档、视频、音频、压缩包等）。未完成的上传将在 10 分钟后自动清理。
          </n-text>
        </n-space>
      </n-card>
    </n-space>
  </page-section>
</template>
