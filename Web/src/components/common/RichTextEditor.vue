<script setup lang="ts">
import '@wangeditor/editor/dist/css/style.css'

import { Editor, Toolbar } from '@wangeditor/editor-for-vue'
import type { IDomEditor, IEditorConfig, IToolbarConfig } from '@wangeditor/editor'
import { onBeforeUnmount, shallowRef } from 'vue'

const model = defineModel<string>({ default: '' })
const editorRef = shallowRef<IDomEditor>()

const toolbarConfig: Partial<IToolbarConfig> = {
  excludeKeys: ['group-video'],
}

const editorConfig: Partial<IEditorConfig> = {
  placeholder: '请输入内容',
}

function handleCreated(editor: IDomEditor) {
  editorRef.value = editor
}

onBeforeUnmount(() => {
  editorRef.value?.destroy()
})
</script>

<template>
  <div class="editor-shell">
    <toolbar :editor="editorRef" :default-config="toolbarConfig" mode="default" />
    <editor
      v-model="model"
      class="editor-shell__content"
      :default-config="editorConfig"
      mode="default"
      @on-created="handleCreated"
    />
  </div>
</template>

<style scoped>
.editor-shell {
  overflow: hidden;
  border: 1px solid rgba(148, 163, 184, 0.3);
  border-radius: 16px;
}

.editor-shell__content {
  min-height: 240px;
}
</style>
