<script setup lang="ts">
import { ElButton } from 'element-plus'
import ErrorAlert from './ErrorAlert.vue'

defineProps<{ title: string; description: string }>()
const emit = defineEmits<{ retry: [] }>()
</script>

<template>
  <div class="load-failure">
    <ErrorAlert :title="title" :description="description" />
    <ElButton class="load-failure__retry" @click="emit('retry')">Try again</ElButton>
  </div>
</template>

<style scoped>
.load-failure {
  margin-bottom: 1rem;
}

.load-failure__retry {
  margin-top: 1rem;
}
</style>
