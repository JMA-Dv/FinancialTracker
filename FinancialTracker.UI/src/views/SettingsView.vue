<script setup lang="ts">
import CurrencyForm from '@/components/CurrencyForm.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
</script>

<template>
  <section class="settings">
    <h1>Settings</h1>
    <p>Signed in as <strong>{{ session.settings?.email }}</strong></p>

    <h2>Currency</h2>
    <p>
      This is only the default for new Accounts. Each Account keeps the Currency it was opened with, and nothing that
      already exists is converted.
    </p>
    <CurrencyForm :current="session.settings?.currency ?? null" />
  </section>
</template>

<style scoped>
.settings {
  max-width: 28rem;
}
</style>
