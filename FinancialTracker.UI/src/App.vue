<script setup lang="ts">
import { ElAlert, ElButton } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import AppShell from '@/components/AppShell.vue'
import { signInLocation } from '@/router'
import { useSessionStore } from '@/stores/session'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()

async function retry(): Promise<void> {
  await session.load()
  if (session.status === 'signedOut') {
    await router.replace(signInLocation(route.fullPath))
  }
}
</script>

<template>
  <template v-if="route.meta.requiresAuth === true && session.status === 'failed'">
    <main class="session-error">
      <ElAlert type="error" show-icon :closable="false" title="Could not load your session" :description="session.failure" />
      <ElButton type="primary" @click="retry">Try again</ElButton>
    </main>
  </template>
  <AppShell v-else-if="route.meta.requiresAuth === true" />
  <RouterView v-else />
</template>

<style scoped>
.session-error {
  display: grid;
  gap: 1rem;
  max-width: 32rem;
  margin: 4rem auto;
  padding: 0 1rem;
}
</style>
