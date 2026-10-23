<script setup lang="ts">
import { ElFormItem, ElInput, ElRadioButton, ElRadioGroup } from 'element-plus'
import type { Period } from '@/composables/useDashboard'
import { periodPresets, type DateRange } from '@/period'

defineProps<{ problem: string }>()
const period = defineModel<Period>('period', { required: true })
const custom = defineModel<DateRange>('custom', { required: true })
</script>

<template>
  <div class="period-picker">
    <ElFormItem label="Period">
      <ElRadioGroup v-model="period">
        <ElRadioButton v-for="preset in periodPresets" :key="preset.id" :value="preset.id">{{ preset.label }}</ElRadioButton>
        <ElRadioButton value="custom">Custom</ElRadioButton>
      </ElRadioGroup>
    </ElFormItem>
    <template v-if="period === 'custom'">
      <ElFormItem label="From">
        <ElInput v-model="custom.from" type="date" />
      </ElFormItem>
      <ElFormItem label="To">
        <ElInput v-model="custom.to" type="date" />
      </ElFormItem>
    </template>
    <p v-if="problem" role="status" class="period-picker__problem">{{ problem }}</p>
  </div>
</template>

<style scoped>
.period-picker {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 0 1rem;
}

.period-picker .el-form-item {
  flex-direction: column;
  margin-bottom: 0.5rem;
}

.period-picker__problem {
  flex-basis: 100%;
  margin: 0 0 0.5rem;
  color: var(--el-color-danger);
  font-size: var(--el-font-size-small);
}
</style>
