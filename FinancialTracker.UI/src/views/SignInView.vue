<script setup lang="ts">
import { ElButton } from 'element-plus'
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { localPath } from '@/router/localPath'

const route = useRoute()

const signInUrl = computed<string>(
  () => `/api/auth/google/sign-in?returnUrl=${encodeURIComponent(localPath(route.query.returnUrl))}`,
)
</script>

<template>
  <main class="sign-in">
    <h1>Finance Tracker</h1>
    <p>Sign in to see your finances.</p>
    <ElButton tag="a" type="primary" :href="signInUrl">Sign in with Google</ElButton>
  </main>
</template>

<style scoped>
.sign-in {
  max-width: 24rem;
  margin: 4rem auto;
  padding: 0 1rem;
  text-align: center;
}
</style>
