<script setup lang="ts">
import { ElButton, ElDialog, ElForm } from 'element-plus'
import ErrorAlert from './ErrorAlert.vue'

defineProps<{ title: string; pending: boolean; canSave: boolean; error: string }>()
const emit = defineEmits<{ close: []; submit: [] }>()
</script>

<template>
  <ElDialog :model-value="true" :title="title" width="min(28rem, 92vw)" @close="emit('close')">
    <ElForm label-position="top" @submit.prevent="emit('submit')">
      <ErrorAlert v-if="error" :title="error" class="form-dialog__error" />
      <slot />
      <ElButton type="primary" native-type="submit" :loading="pending" :disabled="!canSave">Save</ElButton>
      <ElButton @click="emit('close')">Cancel</ElButton>
    </ElForm>
  </ElDialog>
</template>

<style scoped>
.form-dialog__error {
  margin-bottom: 1rem;
}
</style>
