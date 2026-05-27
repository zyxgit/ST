<script setup lang="ts">
import {
  ExpandOutline,
  CheckmarkOutline,
  ContractOutline,
  LockClosedOutline,
  LogOutOutline,
  MenuOutline,
  MoonOutline,
  PersonCircleOutline,
  SettingsOutline,
  SunnyOutline,
} from '@vicons/ionicons5'
import type { FormInst, FormRules } from 'naive-ui'
import { NAvatar, NBreadcrumb, NBreadcrumbItem, NButton, NDivider, NDrawer, NDrawerContent, NDropdown, NForm, NFormItem, NIcon, NImage, NInput, NModal, NSelect, NSpace, NSwitch, NTag } from 'naive-ui'
import { computed, h, nextTick, onBeforeUnmount, reactive, ref } from 'vue'
import { useFullscreen } from '@vueuse/core'
import { useRoute } from 'vue-router'

import AppTopNav from './AppTopNav.vue'

import AvatarCropperModal from '@/components/common/AvatarCropperModal.vue'
import { changeMyEmail, changeMyPassword, checkEmailExists, deleteUserAvatar, getUserDetail, sendEmailCode, setUserAvatar, updateUser } from '@/api/user'
import { uploadFile } from '@/api/file'
import { buildBreadcrumbs } from '@/lib/admin-menu'
import { emailRule, requiredRule } from '@/lib/form-rules'
import { useDiscrete } from '@/lib/naive'
import { useAppStore, type NavigationMode, type ThemeMode } from '@/stores/app'
import { useAuthStore } from '@/stores/auth'

const CHANGE_EMAIL_CODE_PURPOSE = 4

const route = useRoute()
const appStore = useAppStore()
const authStore = useAuthStore()
const { isFullscreen, toggle: toggleFullscreen } = useFullscreen()
const { message } = useDiscrete()

const colorPresets = ['#2563eb', '#ef4444', '#f97316', '#f59e0b', '#06b6d4', '#22c55e', '#3b82f6', '#7c3aed']
const themeModeOptions: Array<{ value: ThemeMode; label: string; description: string }> = [
  { value: 'light', label: '浅色', description: '明亮清爽的白色风格' },
  { value: 'dark', label: '暗黑', description: '更沉浸的深色界面' },
  { value: 'system', label: '跟随系统', description: '自动同步系统明暗主题' },
]
const navigationModeOptions: Array<{ value: NavigationMode; label: string; description: string }> = [
  { value: 'mix', label: '混合导航', description: '顶部模块 + 左侧菜单' },
  { value: 'side', label: '左侧菜单', description: '全部导航集中在侧栏' },
  { value: 'top', label: '顶部菜单', description: '隐藏侧栏，顶部横向导航' },
]
const contentWidthOptions = [
  { label: 'Fluid', value: 'fluid' },
  { label: 'Fixed', value: 'fixed' },
]
const routeAnimationOptions = [
  { label: 'Null', value: 'none' },
  { label: 'Fade', value: 'fade' },
  { label: 'Slide Up', value: 'slide-up' },
  { label: 'Slide Right', value: 'slide-right' },
  { label: 'Zoom Fade', value: 'zoom-fade' },
  { label: 'Blur', value: 'blur' },
]
const breadcrumbs = computed(() => buildBreadcrumbs(route.path, authStore.menuTree))
const userName = computed(() => authStore.currentUser.nickName || '管理员')
const userInitial = computed(() => userName.value.slice(0, 1))
const avatarUrl = computed(() => {
  const fileId = authStore.currentUser.avatarFileId
  if (!fileId) return undefined
  const base = import.meta.env.VITE_API_BASE_URL
  const prefix = base?.startsWith('http') ? base.replace(/\/+$/, '') : ''
  return `${prefix}/api/files/${fileId}/public/download`
})
const showSettingsDrawer = ref(false)
const showProfileModal = ref(false)
const savingProfile = ref(false)
const uploadingAvatar = ref(false)
const showCropper = ref(false)
const cropperImageUrl = ref('')
const avatarFileInput = ref<HTMLInputElement | null>(null)
const profileFormRef = ref<FormInst | null>(null)
const originalEmail = ref('')
const currentEmailCooldown = ref(0)
const newEmailCooldown = ref(0)
let currentEmailTimer: number | null = null
let newEmailTimer: number | null = null
const profileForm = reactive({
  nickName: '',
  email: '',
  phone: '',
  roleIds: [] as string[],
  isEnable: true,
  currentEmailVerifyCode: '',
  newEmailVerifyCode: '',
})
const isEmailChanged = computed(() => profileForm.email.trim().toLowerCase() !== originalEmail.value)
const profileRules = computed<FormRules>(() => ({
  nickName: [requiredRule('昵称')],
  email: [
    emailRule(),
    {
      trigger: ['blur'],
      async validator(_rule, value: unknown) {
        if (typeof value !== 'string' || !value.trim()) {
          throw new Error('请输入邮箱')
        }

        const normalized = value.trim().toLowerCase()
        if (normalized === originalEmail.value) {
          return
        }

        const result = await checkEmailExists(normalized, authStore.currentUser.userId)
        if (result.exists) {
          throw new Error('邮箱已存在')
        }
      },
    },
  ],
  ...(isEmailChanged.value
    ? {
        currentEmailVerifyCode: [requiredRule('当前邮箱验证码')],
        newEmailVerifyCode: [requiredRule('新邮箱验证码')],
      }
    : {}),
}))

const showChangePasswordModal = ref(false)
const savingPassword = ref(false)
const changePasswordFormRef = ref<FormInst | null>(null)
const changePasswordForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
})
const changePasswordRules: FormRules = {
  oldPassword: [requiredRule('原密码')],
  newPassword: [
    requiredRule('新密码'),
    { min: 6, message: '密码至少 6 位', trigger: 'blur' },
  ],
  confirmPassword: [
    requiredRule('确认密码'),
    {
      trigger: ['blur', 'input'],
      validator: (_rule, value: string) => {
        if (value !== changePasswordForm.newPassword) {
          return new Error('两次输入的密码不一致')
        }
        return true
      },
    },
  ],
}

const userOptions = [
  {
    label: '个人信息',
    key: 'profile',
    icon: () => h(NIcon, null, { default: () => h(PersonCircleOutline) }),
  },
  {
    label: '修改密码',
    key: 'change-password',
    icon: () => h(NIcon, null, { default: () => h(LockClosedOutline) }),
  },
  {
    label: '退出登录',
    key: 'logout',
    icon: () => h(NIcon, null, { default: () => h(LogOutOutline) }),
  },
]

async function handleOptionSelect(key: string) {
  if (key === 'profile') {
    await openProfileModal()
    return
  }

  if (key === 'change-password') {
    showChangePasswordModal.value = true
    await nextTick()
    changePasswordFormRef.value?.restoreValidation()
    return
  }

  if (key === 'logout') {
    await authStore.logout()
  }
}

async function openProfileModal() {
  if (!authStore.currentUser.userId) {
    message.warning('当前用户信息未加载完成')
    return
  }

  const detail = await getUserDetail(authStore.currentUser.userId)
  profileForm.nickName = detail.nickName
  profileForm.email = detail.email
  profileForm.phone = detail.phone
  profileForm.roleIds = detail.roles.map((item) => item.id)
  profileForm.isEnable = detail.isEnable
  profileForm.currentEmailVerifyCode = ''
  profileForm.newEmailVerifyCode = ''
  originalEmail.value = detail.email.trim().toLowerCase()
  showProfileModal.value = true
  await nextTick()
  profileFormRef.value?.restoreValidation()
}

async function handleAvatarUpload(file: File) {
  if (!authStore.currentUser.userId) {
    return
  }

  uploadingAvatar.value = true

  try {
    const result = await uploadFile(file, 0) // accessLevel=0 (Public)
    await setUserAvatar(authStore.currentUser.userId, { avatarFileId: result.id })
    authStore.patchCurrentUser({ avatarFileId: result.id })
    message.success('头像已更新')
  } catch {
    message.error('头像上传失败')
  } finally {
    uploadingAvatar.value = false
  }
}

async function handleAvatarSelect(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) {
    return
  }

  if (!file.type.startsWith('image/')) {
    message.warning('请选择图片文件')
    return
  }

  if (file.size > 5 * 1024 * 1024) {
    message.warning('头像图片不能超过 5MB')
    return
  }

  cropperImageUrl.value = URL.createObjectURL(file)
  showCropper.value = true
  input.value = ''
}

async function handleCropConfirm(blob: Blob) {
  const file = new File([blob], 'avatar.png', { type: 'image/png' })
  await handleAvatarUpload(file)
  showCropper.value = false
  URL.revokeObjectURL(cropperImageUrl.value)
  cropperImageUrl.value = ''
}

function handleCropperClose() {
  showCropper.value = false
  URL.revokeObjectURL(cropperImageUrl.value)
  cropperImageUrl.value = ''
}

async function handleRemoveAvatar() {
  if (!authStore.currentUser.userId) {
    return
  }

  await deleteUserAvatar(authStore.currentUser.userId)
  authStore.patchCurrentUser({ avatarFileId: null })
  message.success('头像已移除')
}

async function validateNewEmailAvailable() {
  const email = profileForm.email.trim().toLowerCase()
  if (!email) {
    message.warning('请先输入新邮箱')
    return false
  }

  if (email === originalEmail.value) {
    message.warning('新邮箱不能与当前邮箱相同')
    return false
  }

  const result = await checkEmailExists(email, authStore.currentUser.userId)
  if (result.exists) {
    message.error('该邮箱已被占用')
    return false
  }

  return true
}

function clearCooldownTimer(type: 'current' | 'new') {
  if (type === 'current' && currentEmailTimer !== null) {
    window.clearInterval(currentEmailTimer)
    currentEmailTimer = null
  }

  if (type === 'new' && newEmailTimer !== null) {
    window.clearInterval(newEmailTimer)
    newEmailTimer = null
  }
}

function startCooldown(type: 'current' | 'new') {
  clearCooldownTimer(type)

  if (type === 'current') {
    currentEmailCooldown.value = 60
    currentEmailTimer = window.setInterval(() => {
      if (currentEmailCooldown.value <= 1) {
        currentEmailCooldown.value = 0
        clearCooldownTimer('current')
        return
      }

      currentEmailCooldown.value -= 1
    }, 1000)
    return
  }

  newEmailCooldown.value = 60
  newEmailTimer = window.setInterval(() => {
    if (newEmailCooldown.value <= 1) {
      newEmailCooldown.value = 0
      clearCooldownTimer('new')
      return
    }

    newEmailCooldown.value -= 1
  }, 1000)
}

async function handleSendCurrentEmailCode() {
  if (currentEmailCooldown.value > 0) {
    return
  }

  await sendEmailCode({
    email: originalEmail.value,
    codePurpose: CHANGE_EMAIL_CODE_PURPOSE,
  })
  startCooldown('current')
  message.success('验证码已发送到当前邮箱')
}

async function handleSendNewEmailCode() {
  if (newEmailCooldown.value > 0) {
    return
  }

  const available = await validateNewEmailAvailable()
  if (!available) {
    return
  }

  await sendEmailCode({
    email: profileForm.email.trim().toLowerCase(),
    codePurpose: CHANGE_EMAIL_CODE_PURPOSE,
  })
  startCooldown('new')
  message.success('验证码已发送到新邮箱')
}

async function handleProfileSave() {
  if (!authStore.currentUser.userId) {
    return
  }

  try {
    await profileFormRef.value?.validate()
  } catch {
    return
  }

  savingProfile.value = true

  try {
    await updateUser(authStore.currentUser.userId, {
      nickName: profileForm.nickName.trim(),
      email: isEmailChanged.value ? originalEmail.value : profileForm.email.trim(),
      phone: profileForm.phone.trim() || null,
      roleIds: profileForm.roleIds,
      isEnable: profileForm.isEnable,
    })

    if (isEmailChanged.value) {
      await changeMyEmail({
        newEmail: profileForm.email.trim(),
        currentEmailVerifyCode: profileForm.currentEmailVerifyCode.trim(),
        newEmailVerifyCode: profileForm.newEmailVerifyCode.trim(),
      })
    }

    authStore.patchCurrentUser({
      nickName: profileForm.nickName.trim(),
      email: profileForm.email.trim(),
    })

    showProfileModal.value = false
    message.success('个人信息已更新')
  } finally {
    savingProfile.value = false
  }
}

async function handleChangePasswordSave() {
  try {
    await changePasswordFormRef.value?.validate()
  } catch {
    return
  }

  savingPassword.value = true

  try {
    await changeMyPassword({
      oldPassword: changePasswordForm.oldPassword,
      newPassword: changePasswordForm.newPassword,
    })
    showChangePasswordModal.value = false
    changePasswordForm.oldPassword = ''
    changePasswordForm.newPassword = ''
    changePasswordForm.confirmPassword = ''
    message.success('密码已修改，请重新登录')
  } catch {
    message.error('密码修改失败')
  } finally {
    savingPassword.value = false
  }
}

onBeforeUnmount(() => {
  clearCooldownTimer('current')
  clearCooldownTimer('new')
  if (cropperImageUrl.value) {
    URL.revokeObjectURL(cropperImageUrl.value)
  }
})

function resolveIsDark(mode: ThemeMode) {
  if (mode === 'system') {
    return window.matchMedia('(prefers-color-scheme: dark)').matches
  }

  return mode === 'dark'
}

async function applyThemeMode(mode: ThemeMode, event?: MouseEvent) {
  if (appStore.themeMode === mode) {
    return
  }

  const currentIsDark = appStore.isDark
  const nextIsDark = resolveIsDark(mode)
  const supportsTransition =
    'startViewTransition' in document &&
    !window.matchMedia('(prefers-reduced-motion: reduce)').matches

  if (!supportsTransition || currentIsDark === nextIsDark) {
    appStore.setThemeMode(mode)
    return
  }

  const targetTheme = nextIsDark ? 'to-dark' : 'to-light'
  document.documentElement.dataset.themeTransition = targetTheme

  const target = (event?.currentTarget as HTMLElement | null) ?? null
  const rect = target?.getBoundingClientRect()
  const x = rect ? rect.left + rect.width / 2 : window.innerWidth - 80
  const y = rect ? rect.top + rect.height / 2 : 40
  const endRadius = Math.hypot(
    Math.max(x, window.innerWidth - x),
    Math.max(y, window.innerHeight - y),
  )

  const transition = (document as Document & {
    startViewTransition: (callback: () => void | Promise<void>) => {
      ready: Promise<void>
    }
  }).startViewTransition(async () => {
    appStore.setThemeMode(mode)
    await nextTick()
  })

  await transition.ready

  const animation = document.documentElement.animate(
    {
      clipPath: nextIsDark
        ? [
            `circle(0px at ${x}px ${y}px)`,
            `circle(${endRadius}px at ${x}px ${y}px)`,
          ]
        : [
            `circle(${endRadius}px at ${x}px ${y}px)`,
            `circle(0px at ${x}px ${y}px)`,
          ],
    },
    {
      duration: 450,
      easing: 'cubic-bezier(0.4, 0, 0.2, 1)',
      fill: 'both',
      pseudoElement: nextIsDark
        ? '::view-transition-new(root)'
        : '::view-transition-old(root)',
    },
  )

  animation.finished.finally(() => {
    delete document.documentElement.dataset.themeTransition
  })
}

async function handleThemeToggle(event: MouseEvent) {
  await applyThemeMode(appStore.isDark ? 'light' : 'dark', event)
}
</script>

<template>
  <header v-bind="$attrs" class="app-header" :class="{ 'app-header--compact': appStore.navigationMode === 'side' }">
    <div class="app-header__left">
      <n-button v-if="appStore.navigationMode !== 'top'" quaternary circle @click="appStore.toggleCollapsed">
        <template #icon>
          <n-icon><menu-outline /></n-icon>
        </template>
      </n-button>
      <div class="app-header__title">
        <strong>{{ String(route.meta.title ?? '管理后台') }}</strong>
        <n-breadcrumb>
          <n-breadcrumb-item v-for="item in breadcrumbs" :key="item">
            {{ item }}
          </n-breadcrumb-item>
        </n-breadcrumb>
      </div>
    </div>

    <div v-if="appStore.navigationMode !== 'side'" class="app-header__center">
      <app-top-nav />
    </div>

    <div class="app-header__right">
      <n-tag round type="success">在线</n-tag>
      <n-button quaternary circle @click="showSettingsDrawer = true">
        <template #icon>
          <n-icon><settings-outline /></n-icon>
        </template>
      </n-button>
      <n-button quaternary circle @click="toggleFullscreen()">
        <template #icon>
          <n-icon>
            <component :is="isFullscreen ? ContractOutline : ExpandOutline" />
          </n-icon>
        </template>
      </n-button>
      <n-button quaternary circle @click="handleThemeToggle">
        <template #icon>
          <n-icon>
            <component :is="appStore.isDark ? SunnyOutline : MoonOutline" />
          </n-icon>
        </template>
      </n-button>
      <n-dropdown :options="userOptions" @select="handleOptionSelect">
        <div class="user-entry">
          <n-avatar
            v-if="avatarUrl"
            round
            :src="avatarUrl"
          />
          <n-avatar
            v-else
            round
            :style="{ backgroundColor: appStore.primaryColor }"
          >
            {{ userInitial }}
          </n-avatar>
          <div class="user-entry__meta">
            <strong>{{ userName }}</strong>
            <span>{{ authStore.currentUser.email || '未登录邮箱' }}</span>
          </div>
        </div>
      </n-dropdown>
    </div>
  </header>

  <n-drawer v-model:show="showSettingsDrawer" :width="360" placement="right">
    <n-drawer-content title="整体风格设置" body-content-style="padding: 20px 22px 28px">
      <section class="setting-section">
        <div class="setting-section__title">整体风格</div>
        <div class="setting-theme-grid">
          <button
            v-for="item in themeModeOptions"
            :key="item.value"
            class="setting-theme-card"
            :class="{ 'setting-theme-card--active': appStore.themeMode === item.value }"
            type="button"
            @click="applyThemeMode(item.value)"
          >
            <span class="setting-theme-card__preview" :class="`setting-theme-card__preview--${item.value}`" />
            <span class="setting-theme-card__label">{{ item.label }}</span>
            <n-icon v-if="appStore.themeMode === item.value" class="setting-theme-card__check" size="16">
              <checkmark-outline />
            </n-icon>
          </button>
        </div>
      </section>

      <n-divider />

      <section class="setting-section">
        <div class="setting-section__title">主题色</div>
        <div class="theme-palette">
          <button
            v-for="color in colorPresets"
            :key="color"
            class="theme-palette__item"
            :class="{ 'theme-palette__item--active': color === appStore.primaryColor }"
            :style="{ background: color }"
            type="button"
            @click="appStore.setPrimaryColor(color)"
          >
            <n-icon v-if="color === appStore.primaryColor" color="#ffffff" size="14">
              <checkmark-outline />
            </n-icon>
          </button>
        </div>
      </section>

      <n-divider />

      <section class="setting-section">
        <div class="setting-section__title">导航模式</div>
        <div class="setting-nav-grid">
          <button
            v-for="item in navigationModeOptions"
            :key="item.value"
            class="setting-nav-card"
            :class="{ 'setting-nav-card--active': appStore.navigationMode === item.value }"
            type="button"
            @click="appStore.setNavigationMode(item.value)"
          >
            <span class="setting-nav-card__preview" :class="`setting-nav-card__preview--${item.value}`">
              <span class="setting-nav-card__bar setting-nav-card__bar--dark" />
              <span class="setting-nav-card__bar setting-nav-card__bar--light" />
            </span>
            <span class="setting-nav-card__label">{{ item.label }}</span>
          </button>
        </div>

        <div class="setting-row">
          <span>内容区域宽度</span>
          <n-select
            :consistent-menu-width="false"
            :value="appStore.contentWidth"
            :options="contentWidthOptions"
            style="width: 128px"
            @update:value="(value) => appStore.setContentWidth(value)"
          />
        </div>

        <div class="setting-row">
          <span>固定 Header</span>
          <n-switch :value="appStore.fixedHeader" @update:value="appStore.setFixedHeader" />
        </div>

        <div class="setting-row">
          <span>固定侧边菜单</span>
          <n-switch :value="appStore.fixedSidebar" @update:value="appStore.setFixedSidebar" />
        </div>
      </section>

      <n-divider />

      <section class="setting-section">
        <div class="setting-section__title">其他设置</div>

        <div class="setting-row">
          <span>路由动画</span>
          <n-select
            :consistent-menu-width="false"
            :value="appStore.routeAnimation"
            :options="routeAnimationOptions"
            style="width: 128px"
            @update:value="(value) => appStore.setRouteAnimation(value)"
          />
        </div>

        <div class="setting-row">
          <span>多标签</span>
          <n-switch :value="appStore.multiTabs" @update:value="appStore.setMultiTabs" />
        </div>

        <div class="setting-row">
          <span>固定多标签</span>
          <n-switch
            :value="appStore.fixedTabs"
            :disabled="!appStore.multiTabs"
            @update:value="appStore.setFixedTabs"
          />
        </div>

        <div class="setting-row">
          <span>色弱模式</span>
          <n-switch :value="appStore.colorWeakMode" @update:value="appStore.setColorWeakMode" />
        </div>
      </section>
    </n-drawer-content>
  </n-drawer>

  <input
    ref="avatarFileInput"
    type="file"
    accept="image/*"
    style="display: none"
    @change="handleAvatarSelect"
  >

  <n-modal v-model:show="showProfileModal" preset="card" style="width: 520px" title="个人信息">
    <div style="display: flex; align-items: center; gap: 16px; margin-bottom: 20px;">
      <n-image
        v-if="avatarUrl"
        :src="avatarUrl"
        :width="72"
        :height="72"
        :previewed-img-props="{ style: { objectFit: 'contain' } }"
        :img-props="{ style: { objectFit: 'cover', borderRadius: '50%', width: '72px', height: '72px' } }"
        style="border-radius: 50%; overflow: hidden; flex-shrink: 0;"
      />
      <n-avatar
        v-else
        round
        :size="72"
        :style="{ backgroundColor: appStore.primaryColor, fontSize: '28px' }"
      >
        {{ userInitial }}
      </n-avatar>
      <n-space>
        <n-button :loading="uploadingAvatar" @click="avatarFileInput?.click()">
          上传头像
        </n-button>
        <n-button v-if="avatarUrl" @click="handleRemoveAvatar">
          移除头像
        </n-button>
      </n-space>
    </div>
    <n-form ref="profileFormRef" :model="profileForm" :rules="profileRules" label-placement="top">
      <n-form-item label="昵称" path="nickName">
        <n-input v-model:value="profileForm.nickName" />
      </n-form-item>
      <n-form-item label="当前邮箱">
        <n-input :value="originalEmail" readonly />
      </n-form-item>
      <n-form-item label="新邮箱" path="email">
        <n-input v-model:value="profileForm.email" />
      </n-form-item>
      <n-form-item v-if="isEmailChanged" label="当前邮箱验证码" path="currentEmailVerifyCode">
        <n-space style="width: 100%" :wrap="false">
          <n-input v-model:value="profileForm.currentEmailVerifyCode" placeholder="请输入当前邮箱收到的验证码" />
          <n-button :disabled="currentEmailCooldown > 0" @click="handleSendCurrentEmailCode">
            {{ currentEmailCooldown > 0 ? `${currentEmailCooldown}s` : '发送验证码' }}
          </n-button>
        </n-space>
      </n-form-item>
      <n-form-item v-if="isEmailChanged" label="新邮箱验证码" path="newEmailVerifyCode">
        <n-space style="width: 100%" :wrap="false">
          <n-input v-model:value="profileForm.newEmailVerifyCode" placeholder="请输入新邮箱收到的验证码" />
          <n-button :disabled="newEmailCooldown > 0" @click="handleSendNewEmailCode">
            {{ newEmailCooldown > 0 ? `${newEmailCooldown}s` : '发送验证码' }}
          </n-button>
        </n-space>
      </n-form-item>
      <n-form-item label="手机号" path="phone">
        <n-input v-model:value="profileForm.phone" />
      </n-form-item>
    </n-form>
    <template #footer>
      <div class="profile-modal__footer">
        <n-button @click="showProfileModal = false">取消</n-button>
        <n-button type="primary" :loading="savingProfile" @click="handleProfileSave">保存</n-button>
      </div>
    </template>
  </n-modal>

  <n-modal v-model:show="showChangePasswordModal" preset="card" style="width: 420px" title="修改密码">
    <n-form ref="changePasswordFormRef" :model="changePasswordForm" :rules="changePasswordRules" label-placement="top">
      <n-form-item label="原密码" path="oldPassword">
        <n-input v-model:value="changePasswordForm.oldPassword" type="password" show-password-on="click" placeholder="请输入原密码" />
      </n-form-item>
      <n-form-item label="新密码" path="newPassword">
        <n-input v-model:value="changePasswordForm.newPassword" type="password" show-password-on="click" placeholder="请输入新密码（至少 6 位）" />
      </n-form-item>
      <n-form-item label="确认密码" path="confirmPassword">
        <n-input v-model:value="changePasswordForm.confirmPassword" type="password" show-password-on="click" placeholder="请再次输入新密码" />
      </n-form-item>
    </n-form>
    <template #footer>
      <div style="display: flex; justify-content: flex-end; gap: 12px;">
        <n-button @click="showChangePasswordModal = false">取消</n-button>
        <n-button type="primary" :loading="savingPassword" @click="handleChangePasswordSave">保存</n-button>
      </div>
    </template>
  </n-modal>

  <avatar-cropper-modal
    v-model:visible="showCropper"
    :image-url="cropperImageUrl"
    :uploading="uploadingAvatar"
    @crop="handleCropConfirm"
    @update:visible="handleCropperClose"
  />
</template>

<style scoped>
.app-header {
  display: grid;
  grid-template-columns: 280px 1fr auto;
  align-items: center;
  gap: 18px;
  min-height: 72px;
  padding: 0 20px;
  border-bottom: 1px solid var(--panel-border);
  background: var(--panel-bg);
}

.app-header--compact {
  grid-template-columns: minmax(0, 1fr) auto;
}

.app-header__left {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.app-header__title {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 4px;
}

.app-header__title strong {
  color: var(--text-1);
  font-size: 18px;
}

.app-header__center {
  min-width: 0;
}

.app-header__right {
  display: flex;
  align-items: center;
  gap: 10px;
  justify-self: end;
}

.setting-section__title {
  margin-bottom: 14px;
  color: var(--text-1);
  font-size: 20px;
  font-weight: 700;
}

.setting-theme-grid,
.setting-nav-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.setting-theme-card,
.setting-nav-card {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 8px;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  cursor: pointer;
  transition:
    border-color 0.2s ease,
    transform 0.2s ease,
    box-shadow 0.2s ease;
}

.setting-theme-card:hover,
.setting-nav-card:hover {
  transform: translateY(-1px);
  box-shadow: var(--shadow-1);
}

.setting-theme-card--active,
.setting-nav-card--active {
  border-color: color-mix(in srgb, var(--n-primary-color, #2563eb) 70%, white 30%);
}

.setting-theme-card__preview,
.setting-nav-card__preview {
  position: relative;
  display: block;
  height: 52px;
  overflow: hidden;
  border-radius: 10px;
  border: 1px solid rgba(148, 163, 184, 0.18);
}

.setting-theme-card__preview--light {
  background: linear-gradient(135deg, #ffffff 0%, #e5e7eb 100%);
}

.setting-theme-card__preview--light::before {
  content: '';
  position: absolute;
  inset: 0 auto 0 0;
  width: 34%;
  background: #f3f4f6;
}

.setting-theme-card__preview--dark {
  background: linear-gradient(135deg, #0f172a 0%, #1f2937 100%);
}

.setting-theme-card__preview--dark::before {
  content: '';
  position: absolute;
  inset: 0 auto 0 0;
  width: 34%;
  background: #082f49;
}

.setting-theme-card__preview--system {
  background: linear-gradient(90deg, #f8fafc 0%, #f8fafc 50%, #1f2937 50%, #1f2937 100%);
}

.setting-theme-card__preview--system::before {
  content: '';
  position: absolute;
  inset: 0 auto 0 0;
  width: 18%;
  background: #e2e8f0;
}

.setting-theme-card__preview--system::after {
  content: '';
  position: absolute;
  inset: 0 0 0 auto;
  width: 18%;
  background: #0f172a;
}

.setting-theme-card__label,
.setting-nav-card__label {
  color: var(--text-2);
  font-size: 13px;
}

.setting-theme-card__check {
  position: absolute;
  right: 10px;
  bottom: 10px;
  color: var(--n-primary-color, #2563eb);
}

.theme-palette {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.setting-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 10px 0;
}

.setting-row span {
  color: var(--text-2);
  font-size: 15px;
}

.theme-palette__item {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  box-shadow: inset 0 0 0 2px rgba(255, 255, 255, 0.85);
}

.theme-palette__item--active {
  outline: 2px solid var(--text-1);
  outline-offset: 2px;
}

.setting-nav-card__preview {
  background: #f8fafc;
}

.setting-nav-card__preview--mix .setting-nav-card__bar--dark {
  position: absolute;
  inset: 0 auto 0 0;
  width: 28%;
  background: #082f49;
}

.setting-nav-card__preview--mix .setting-nav-card__bar--light {
  position: absolute;
  inset: 0 0 auto 28%;
  height: 28%;
  background: #0f172a;
}

.setting-nav-card__preview--side .setting-nav-card__bar--dark {
  position: absolute;
  inset: 0 auto 0 0;
  width: 30%;
  background: #082f49;
}

.setting-nav-card__preview--side .setting-nav-card__bar--light {
  position: absolute;
  inset: 0 0 0 30%;
  background: #f8fafc;
}

.setting-nav-card__preview--top .setting-nav-card__bar--dark {
  position: absolute;
  inset: 0 0 auto 0;
  height: 28%;
  background: #0f172a;
}

.setting-nav-card__preview--top .setting-nav-card__bar--light {
  position: absolute;
  inset: 28% 0 0 0;
  background: #f8fafc;
}

.user-entry {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 6px 10px;
  border: 1px solid var(--panel-border);
  border-radius: 14px;
  background: var(--panel-bg-strong);
  cursor: pointer;
}

.user-entry__meta {
  display: flex;
  flex-direction: column;
}

.user-entry__meta strong {
  color: var(--text-1);
  font-size: 14px;
}

.user-entry__meta span {
  color: var(--text-3);
  font-size: 12px;
}

.profile-modal__footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
