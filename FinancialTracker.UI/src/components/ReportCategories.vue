<script setup lang="ts">
import { ElButton, ElTag } from 'element-plus'
import { ref } from 'vue'
import { formatMoney } from '@/money'
import type { DateRange } from '@/period'
import type { CategoryTotal } from '@/reports'

defineProps<{ caption: string; categories: CategoryTotal[]; currency: string; range: DateRange | null }>()

const expanded = ref(new Set<string>())

function toggle(id: string): void {
  const next = new Set(expanded.value)
  if (!next.delete(id)) {
    next.add(id)
  }
  expanded.value = next
}
</script>

<template>
  <div class="report-scroll">
    <table class="report-table">
      <caption class="report-table__caption">{{ caption }}</caption>
      <thead>
        <tr>
          <th scope="col">Category</th>
          <th scope="col">Total</th>
          <th scope="col">Recorded directly on it</th>
        </tr>
      </thead>
      <tbody>
        <template v-for="parent in categories" :key="parent.id">
          <tr>
            <td>
              <ElButton
                v-if="parent.children.length > 0"
                link
                :aria-expanded="expanded.has(parent.id)"
                :aria-label="`${expanded.has(parent.id) ? 'Hide' : 'Show'} ${parent.name} subcategories`"
                @click="toggle(parent.id)"
              >
                {{ expanded.has(parent.id) ? '▾' : '▸' }}
              </ElButton>
              <RouterLink :to="{ path: '/transactions', query: { categoryId: parent.id, ...range } }">{{ parent.name }}</RouterLink>
              <ElTag v-if="parent.isArchived" type="info" size="small" disable-transitions>Archived</ElTag>
            </td>
            <td>{{ formatMoney(parent.total, currency) }}</td>
            <td>{{ formatMoney(parent.ownAmount, currency) }}</td>
          </tr>
          <template v-if="expanded.has(parent.id)">
            <tr v-for="child in parent.children" :key="child.id" class="report-table__child">
              <td>
                <RouterLink :to="{ path: '/transactions', query: { categoryId: child.id, ...range } }">{{ child.name }}</RouterLink>
                <ElTag v-if="child.isArchived" type="info" size="small" disable-transitions>Archived</ElTag>
              </td>
              <td>{{ formatMoney(child.total, currency) }}</td>
              <td>{{ formatMoney(child.ownAmount, currency) }}</td>
            </tr>
          </template>
        </template>
      </tbody>
    </table>
  </div>
</template>
