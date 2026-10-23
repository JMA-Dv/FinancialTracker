<script setup lang="ts">
import { ElOption, ElSelect } from 'element-plus'
import { currencyOptions } from '@/currencies'

const currency = defineModel<string>({ required: true })
const options = currencyOptions()
</script>

<template>
  <ElSelect v-model="currency" filterable placeholder="Search by code or name">
    <ElOption v-for="option in options" :key="option.code" :value="option.code" :label="option.label" />
  </ElSelect>
</template>
