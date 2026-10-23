<script setup lang="ts">
import { ElButton } from 'element-plus'
import { computed, onMounted, ref } from 'vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import PayeeRow from '@/components/PayeeRow.vue'
import RenameDialog from '@/components/RenameDialog.vue'
import { useCategoriesStore } from '@/stores/categories'
import { usePayeesStore, type Payee } from '@/stores/payees'

const payees = usePayeesStore()
const categories = useCategoriesStore()
const renaming = ref<Payee | null>(null)

// A failed refresh keeps the Payees already shown, and says so above them.
const showList = computed(() => payees.payees.length > 0 || payees.status === 'ready')

onMounted(() => {
  void payees.load()
  // Only for naming the suggested Categories; the list is useful without them, and a failed load is tried again.
  if (categories.status !== 'ready') {
    void categories.load()
  }
})
</script>

<template>
  <section>
    <h1>Payees</h1>

    <div v-if="payees.status === 'failed'" class="payees__alert">
      <ErrorAlert title="Could not load your Payees" :description="payees.failure" />
      <ElButton class="payees__retry" @click="payees.load()">Try again</ElButton>
    </div>

    <p v-if="payees.status === 'idle' || (payees.status === 'loading' && !showList)" role="status">Loading Payees…</p>
    <template v-else-if="showList">
      <p v-if="payees.payees.length === 0">
        No Payees yet. Payees are created by naming them on a Transaction.
      </p>
      <ul v-else class="payees__list">
        <li v-for="payee in payees.payees" :key="payee.id">
          <PayeeRow
            :payee="payee"
            :category-name="payee.suggestedCategoryId === null ? null : categories.nameOf(payee.suggestedCategoryId)"
            @rename="renaming = $event"
          />
        </li>
      </ul>
    </template>

    <RenameDialog v-if="renaming" :target="renaming" :rename="payees.rename" @close="renaming = null" @saved="renaming = null" />
  </section>
</template>

<style scoped>
.payees__list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.payees__alert {
  margin-bottom: 1rem;
}

.payees__retry {
  margin-top: 1rem;
}
</style>
