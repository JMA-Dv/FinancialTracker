<script setup lang="ts">
import { ElButton, ElForm, ElFormItem, ElOption, ElSelect } from 'element-plus'
import { ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import { currencyOptions } from '@/currencies'
import { useSessionStore } from '@/stores/session'
import ErrorAlert from './ErrorAlert.vue'

const emit = defineEmits<{ saved: [] }>()

const session = useSessionStore()
const options = currencyOptions()
const choice = ref<string>(session.settings?.currency ?? '')
const saved = ref(false)

// A refusal leaves the choice in the picker so the User can fix it or try again.
const { pending, error, submit } = useSubmit(async () => {
  saved.value = false
  await session.setCurrency(choice.value)
})

async function save(): Promise<void> {
  if (await submit()) {
    saved.value = true
    emit('saved')
  }
}
</script>

<template>
  <ElForm label-position="top" @submit.prevent="save">
    <!-- Always in the page, so screen readers announce the text when it appears. -->
    <p role="status">{{ saved ? 'Currency saved.' : '' }}</p>
    <ErrorAlert v-if="error" :title="error" class="currency-form__error" />
    <ElFormItem label="Currency">
      <ElSelect v-model="choice" filterable placeholder="Search by code or name">
        <ElOption
          v-for="option in options"
          :key="option.code"
          :value="option.code"
          :label="option.label"
        />
      </ElSelect>
    </ElFormItem>
    <ElButton type="primary" native-type="submit" :loading="pending" :disabled="choice === ''">Save</ElButton>
  </ElForm>
</template>

<style scoped>
.currency-form__error {
  margin-bottom: 1rem;
}
</style>
