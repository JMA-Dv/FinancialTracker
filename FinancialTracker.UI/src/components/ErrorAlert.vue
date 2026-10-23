<script setup lang="ts">
import { ElAlert } from 'element-plus'

defineProps<{ title: string; description?: string }>()
</script>

<template>
  <ElAlert type="error" show-icon :closable="false" :title="title" :description="description" />
</template>
