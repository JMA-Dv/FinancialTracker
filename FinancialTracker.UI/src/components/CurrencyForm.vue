<script setup lang="ts">
import { ElAlert, ElButton, ElForm, ElFormItem, ElOption, ElSelect } from 'element-plus'
import { ref } from 'vue'
import { UnauthorizedError, describeFailure } from '@/api/client'
import { currencyOptions } from '@/currencies'
import { useSessionStore } from '@/stores/session'

const props = defineProps<{ current: string | null }>()
const emit = defineEmits<{ saved: [code: string] }>()

const session = useSessionStore()
const options = currencyOptions()
const choice = ref<string>(props.current ?? '')
const saving = ref(false)
const error = ref('')
const saved = ref(false)

async function save(): Promise<void> {
  saving.value = true
  error.value = ''
  saved.value = false
  try {
    await session.setCurrency(choice.value)
    saved.value = true
    emit('saved', choice.value)
  } catch (failure) {
    // The choice stays in the picker so the User can fix it or try again.
    // A 401 is already sending them to sign-in.
    if (!(failure instanceof UnauthorizedError)) {
      error.value = describeFailure(failure)
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <ElForm label-position="top" @submit.prevent="save">
    <p v-if="saved" role="status">Currency saved.</p>
    <ElAlert v-if="error" type="error" show-icon :closable="false" :title="error" class="currency-form__error" />
    <ElFormItem label="Currency">
      <ElSelect v-model="choice" filterable placeholder="Search by code or name">
        <ElOption
          v-for="option in options"
          :key="option.code"
          :value="option.code"
          :label="`${option.code} – ${option.name}`"
        />
      </ElSelect>
    </ElFormItem>
    <ElButton type="primary" native-type="submit" :loading="saving" :disabled="choice === ''">Save</ElButton>
  </ElForm>
</template>

<style scoped>
.currency-form__error {
  margin-bottom: 1rem;
}
</style>
