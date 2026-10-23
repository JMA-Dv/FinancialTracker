<script setup lang="ts">
import { ElButton } from 'element-plus'
import { useRouter } from 'vue-router'
import { useSubmit } from '@/composables/useSubmit'
import { navItems } from '@/router'
import { useSessionStore } from '@/stores/session'
import ErrorAlert from './ErrorAlert.vue'

const session = useSessionStore()
const router = useRouter()

// A 401 here means the session was already gone; the 401 handler is sending the User to sign-in.
const { error: signOutError, submit: trySignOut } = useSubmit(() => session.signOut())

async function signOut(): Promise<void> {
  if (await trySignOut()) {
    await router.push({ name: 'sign-in' })
  }
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
      <RouterLink to="/settings" class="shell__link">Settings</RouterLink>
      <ElButton @click="signOut">Sign out</ElButton>
    </header>
    <ErrorAlert v-if="signOutError" title="Could not sign out" :description="signOutError" />
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
