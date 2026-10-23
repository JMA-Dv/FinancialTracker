<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import CurrencyForm from '@/components/CurrencyForm.vue'
import { localPath } from '@/router/localPath'

const route = useRoute()
const router = useRouter()

async function continueToDestination(): Promise<void> {
  await router.push(localPath(route.query.returnUrl))
}
</script>

<template>
  <section class="setup">
    <h1>Choose your Currency</h1>
    <p>This is the Currency offered when you open a new Account. Each Account keeps its own, and nothing is converted.</p>
    <CurrencyForm @saved="continueToDestination" />
  </section>
</template>

<style scoped>
.setup {
  max-width: 28rem;
}
</style>
