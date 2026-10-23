<script setup lang="ts">
import { ElAlert, ElButton } from 'element-plus'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiClientError, UnauthorizedError } from '@/api/client'
import { navItems } from '@/router'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
const router = useRouter()

const signOutError = ref('')

async function signOut(): Promise<void> {
  signOutError.value = ''
  try {
    await session.signOut()
  } catch (error) {
    if (!(error instanceof ApiClientError)) {
      throw error
    }
    // The session was already gone; the 401 handler is sending the User to sign-in.
    if (!(error instanceof UnauthorizedError)) {
      signOutError.value = error.message
    }
    return
  }
  await router.push({ name: 'sign-in' })
}
</script>

<template>
  <div class="shell">
    <header class="shell__header">
      <strong class="shell__brand">Finance Tracker</strong>
      <nav class="shell__nav" aria-label="Main">
        <RouterLink v-for="item in navItems" :key="item.to" :to="item.to" class="shell__link">
          {{ item.label }}
        </RouterLink>
      </nav>
      <span class="shell__user">{{ session.settings?.email }}</span>
      <ElButton @click="signOut">Sign out</ElButton>
    </header>
    <ElAlert v-if="signOutError" type="error" show-icon :closable="false" :title="`Could not sign out. ${signOutError}`" />
    <main class="shell__content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.shell__header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem 1.5rem;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--el-border-color);
}

.shell__nav {
  display: flex;
  flex-wrap: wrap;
  flex: 1;
  gap: 0.25rem 1rem;
}

.shell__link {
  color: var(--el-text-color-regular);
  text-decoration: none;
}

.shell__link.router-link-exact-active,
.shell__link:hover,
.shell__link:focus-visible {
  color: var(--el-color-primary);
}

.shell__content {
  max-width: 72rem;
  margin: 0 auto;
  padding: 1rem;
}
</style>
