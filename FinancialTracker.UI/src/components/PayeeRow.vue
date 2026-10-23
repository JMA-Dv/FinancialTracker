<script setup lang="ts">
import { ElButton } from 'element-plus'
import type { Payee } from '@/stores/payees'

defineProps<{ payee: Payee; categoryName: string | null }>()
const emit = defineEmits<{ rename: [payee: Payee] }>()
</script>

<template>
  <div class="payee-row">
    <span class="payee-row__name">{{ payee.name }}</span>
    <span class="payee-row__category">{{ payee.suggestedCategoryId === null ? 'No suggested Category' : (categoryName ?? '') }}</span>
    <ElButton link :aria-label="`Rename ${payee.name}`" @click="emit('rename', payee)">Rename</ElButton>
  </div>
</template>

<style scoped>
.payee-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.25rem 0.75rem;
  padding: 0.375rem 0;
}

.payee-row__category {
  color: var(--el-text-color-secondary);
  font-size: var(--el-font-size-small);
}

.payee-row .el-button {
  margin-left: auto;
}
</style>
