<script setup lang="ts">
import { CheckmarkCircleOutline, LockClosedOutline, MailOutline, ShieldCheckmarkOutline } from '@vicons/ionicons5'
import type { FormInst, FormRules } from 'naive-ui'
import { NButton, NCard, NCheckbox, NForm, NFormItem, NGrid, NGridItem, NIcon, NInput, NText } from 'naive-ui'
import { onMounted, onUnmounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { emailRule, passwordRule } from '@/lib/form-rules'
import { useDiscrete } from '@/lib/naive'
import { useAuthStore } from '@/stores/auth'

const SAVED_LOGIN_KEY = 'st-admin:remembered-login'

type RememberedLogin = {
  email: string
  password: string
}

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { message } = useDiscrete()

const loading = ref(false)
const rememberPassword = ref(false)
const formRef = ref<FormInst | null>(null)
const formValue = reactive({
  email: '',
  password: '',
})
const rules: FormRules = {
  email: [emailRule()],
  password: [passwordRule()],
}

function readRememberedLogin() {
  const raw = localStorage.getItem(SAVED_LOGIN_KEY)

  if (!raw) {
    return
  }

  try {
    const parsed = JSON.parse(raw) as Partial<RememberedLogin>

    if (typeof parsed.email === 'string' && typeof parsed.password === 'string') {
      formValue.email = parsed.email
      formValue.password = parsed.password
      rememberPassword.value = Boolean(parsed.email || parsed.password)
    }
  } catch {
    localStorage.removeItem(SAVED_LOGIN_KEY)
  }
}

function syncRememberedLogin() {
  if (!rememberPassword.value) {
    localStorage.removeItem(SAVED_LOGIN_KEY)
    return
  }

  localStorage.setItem(
    SAVED_LOGIN_KEY,
    JSON.stringify({
      email: formValue.email.trim(),
      password: formValue.password,
    } satisfies RememberedLogin),
  )
}

function handleRememberChange(value: boolean) {
  rememberPassword.value = value

  if (!value) {
    localStorage.removeItem(SAVED_LOGIN_KEY)
  }
}

async function handleSubmit() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }

  loading.value = true

  try {
    await authStore.login({
      email: formValue.email.trim(),
      password: formValue.password.trim(),
    })
    syncRememberedLogin()
    message.success('登录成功')
    await router.replace(String(route.query.redirect ?? '/system/users'))
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  readRememberedLogin()

  document.body.style.minWidth = '0'
  document.body.style.overflow = 'auto'
})

onUnmounted(() => {
  document.body.style.minWidth = ''
  document.body.style.overflow = ''
})
</script>

<template>
  <div class="login-page">
    <div class="login-page__aurora login-page__aurora--one"></div>
    <div class="login-page__aurora login-page__aurora--two"></div>
    <div class="login-page__grid"></div>

    <div class="login-page__panel">
      <section class="login-page__hero">
        <span class="login-page__badge">ST Admin Control Center</span>
        <div class="login-page__headline">
          <p class="login-page__eyebrow">Enterprise Console</p>
          <h1>把模板后台打磨成真正能落地的管理中枢</h1>
          <p class="login-page__description">
            聚合身份认证、用户权限、菜单编排与操作审计，让你的微服务模板从“能跑”升级到“可管理、可扩展、可交付”。
          </p>
        </div>

        <div class="login-page__stats">
          <article class="login-page__stat">
            <strong>Auth</strong>
            <span>统一认证与会话管理</span>
          </article>
          <article class="login-page__stat">
            <strong>RBAC</strong>
            <span>角色、菜单、权限闭环控制</span>
          </article>
          <article class="login-page__stat">
            <strong>Audit</strong>
            <span>关键操作全链路留痕</span>
          </article>
        </div>

        <div class="login-page__feature-list">
          <div class="login-page__feature">
            <n-icon size="18"><shield-checkmark-outline /></n-icon>
            <span>高可信权限边界</span>
          </div>
          <div class="login-page__feature">
            <n-icon size="18"><checkmark-circle-outline /></n-icon>
            <span>开箱即用的后台骨架</span>
          </div>
        </div>
      </section>

      <n-card class="login-page__card" :bordered="false">
        <template #header>
          <div class="login-page__header">
            <div>
              <strong>欢迎回来</strong>
              <n-text depth="3">请使用系统账号登录控制台</n-text>
            </div>
            <span class="login-page__card-tag">Secure Access</span>
          </div>
        </template>

        <n-form ref="formRef" :model="formValue" :rules="rules" @submit.prevent="handleSubmit">
          <n-grid :cols="1" :y-gap="16">
            <n-grid-item>
              <n-form-item label="邮箱" path="email" required>
                <n-input v-model:value="formValue.email" placeholder="示例账号 test@qq.com">
                  <template #prefix>
                    <n-icon><mail-outline /></n-icon>
                  </template>
                </n-input>
              </n-form-item>
            </n-grid-item>
            <n-grid-item>
              <n-form-item label="密码" path="password" required>
                <n-input
                  v-model:value="formValue.password"
                  show-password-on="click"
                  type="password"
                  placeholder="示例密码 123456"
                >
                  <template #prefix>
                    <n-icon><lock-closed-outline /></n-icon>
                  </template>
                </n-input>
              </n-form-item>
            </n-grid-item>
            <n-grid-item>
              <div class="login-page__actions">
                <n-checkbox :checked="rememberPassword" @update:checked="handleRememberChange">
                  保存账号密码
                </n-checkbox>
                <span class="login-page__hint">下次进入自动回填</span>
              </div>
            </n-grid-item>
            <n-grid-item>
              <n-button block type="primary" size="large" attr-type="submit" :loading="loading">
                登录
              </n-button>
            </n-grid-item>
          </n-grid>
        </n-form>
      </n-card>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  --login-bg: #07111f;
  --login-surface: rgba(7, 18, 34, 0.72);
  --login-surface-strong: rgba(8, 22, 41, 0.88);
  --login-border: rgba(148, 163, 184, 0.18);
  --login-text: #e2e8f0;
  --login-text-soft: rgba(226, 232, 240, 0.72);
  --login-accent: #d4af37;
  --login-accent-soft: rgba(212, 175, 55, 0.18);
  position: relative;
  display: grid;
  min-height: 100vh;
  padding: 32px;
  overflow: hidden;
  background:
    radial-gradient(circle at top left, rgba(56, 189, 248, 0.18), transparent 32%),
    radial-gradient(circle at right 20%, rgba(212, 175, 55, 0.18), transparent 28%),
    linear-gradient(135deg, #020617 0%, #07111f 42%, #0f1b2d 100%);
  place-items: center;
}

.login-page__aurora {
  position: absolute;
  border-radius: 999px;
  filter: blur(12px);
  pointer-events: none;
}

.login-page__aurora--one {
  top: 8%;
  left: -8%;
  width: 360px;
  height: 360px;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.26), transparent 70%);
  animation: floatAurora 12s ease-in-out infinite;
}

.login-page__aurora--two {
  right: -10%;
  bottom: 6%;
  width: 420px;
  height: 420px;
  background: radial-gradient(circle, rgba(245, 158, 11, 0.22), transparent 68%);
  animation: floatAurora 14s ease-in-out infinite reverse;
}

.login-page__grid {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(148, 163, 184, 0.08) 1px, transparent 1px),
    linear-gradient(90deg, rgba(148, 163, 184, 0.08) 1px, transparent 1px);
  background-size: 72px 72px;
  mask-image: linear-gradient(180deg, rgba(0, 0, 0, 0.7), transparent 88%);
  pointer-events: none;
}

.login-page__panel {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(360px, 440px);
  width: min(1240px, 100%);
  overflow: hidden;
  border: 1px solid var(--login-border);
  border-radius: 32px;
  background: linear-gradient(135deg, rgba(255, 255, 255, 0.05), rgba(255, 255, 255, 0.02));
  box-shadow:
    0 32px 90px rgba(2, 6, 23, 0.45),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(22px);
}

.login-page__hero {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: 28px;
  padding: 72px 64px;
  color: var(--login-text);
}

.login-page__headline {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.login-page__eyebrow {
  margin: 0;
  color: var(--login-accent);
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.28em;
  text-transform: uppercase;
}

.login-page__hero h1 {
  max-width: 680px;
  margin: 0;
  font-size: clamp(40px, 5vw, 64px);
  line-height: 1.04;
  letter-spacing: -0.03em;
}

.login-page__description {
  max-width: 620px;
  margin: 0;
  color: var(--login-text-soft);
  font-size: 17px;
  line-height: 1.8;
}

.login-page__badge {
  display: inline-flex;
  width: fit-content;
  padding: 8px 14px;
  border: 1px solid rgba(212, 175, 55, 0.24);
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.35);
  color: #f8e7a5;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  backdrop-filter: blur(12px);
}

.login-page__stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

.login-page__stat {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 20px 18px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  border-radius: 22px;
  background: rgba(15, 23, 42, 0.3);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
}

.login-page__stat strong {
  color: #ffffff;
  font-size: 24px;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.login-page__stat span {
  color: var(--login-text-soft);
  font-size: 14px;
}

.login-page__feature-list {
  display: flex;
  flex-wrap: wrap;
  gap: 14px;
}

.login-page__feature {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  border: 1px solid rgba(148, 163, 184, 0.14);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.05);
  color: #f8fafc;
}

.login-page__card {
  display: flex;
  align-items: center;
  padding: 44px 38px;
  border-left: 1px solid rgba(148, 163, 184, 0.12);
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.94), rgba(248, 250, 252, 0.86)),
    linear-gradient(180deg, rgba(212, 175, 55, 0.06), rgba(15, 23, 42, 0));
}

.login-page__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.login-page__header strong {
  display: block;
  margin-bottom: 6px;
  color: #0f172a;
  font-size: 30px;
  line-height: 1.1;
}

.login-page__card-tag {
  flex-shrink: 0;
  padding: 6px 10px;
  border-radius: 999px;
  background: var(--login-accent-soft);
  color: #946200;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.login-page__actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  color: #475569;
}

.login-page__hint {
  color: #94a3b8;
  font-size: 13px;
}

@media (max-width: 1080px) {
  .login-page {
    padding: 20px;
  }

  .login-page__panel {
    grid-template-columns: 1fr;
  }

  .login-page__hero {
    padding: 48px 32px 28px;
  }

  .login-page__card {
    padding: 24px 24px 32px;
    border-top: 1px solid rgba(148, 163, 184, 0.12);
    border-left: 0;
  }
}

@media (max-width: 768px) {
  .login-page {
    padding: 16px;
  }

  .login-page__hero {
    padding: 32px 22px 18px;
  }

  .login-page__stats {
    grid-template-columns: 1fr;
  }

  .login-page__header {
    flex-direction: column;
  }

  .login-page__actions {
    flex-direction: column;
    align-items: flex-start;
  }
}

@keyframes floatAurora {
  0%,
  100% {
    transform: translate3d(0, 0, 0) scale(1);
  }

  50% {
    transform: translate3d(24px, -18px, 0) scale(1.08);
  }
}
</style>
